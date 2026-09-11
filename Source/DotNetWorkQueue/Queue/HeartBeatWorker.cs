// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Queue
{
    /// <inheritdoc />
    public class HeartBeatWorker : IHeartBeatWorker
    {
        #region Member level Variables

        private readonly ILogger _logger;
        private readonly string _checkTime;
        //The window the monitor resets against. A message whose stored heartbeat is older than this is
        //treated as abandoned and handed to another worker, so it is also the deadline this worker has
        //to keep meeting to keep its claim.
        private readonly TimeSpan _expiry;
        //Ticks of the last beat that actually landed. Long, not DateTime, so it can be read and written
        //without the beat lock - the staleness check runs on ticks where the beat itself is skipped.
        private long _lastGoodBeatUtcTicks;
        //Set when a scheduled tick arrives while a beat is already running. That slot would otherwise be
        //forfeited until the next tick, and with a 3 second schedule against a 10 second expiry there
        //are only about three slots in the window - losing them is how a live worker's claim lapses.
        private int _missedTick;
        private readonly ISendHeartBeat _sendHeartbeat;
        private readonly IMessageContext _context;
        private CancellationTokenSource _cancel;
        private readonly object _cancelLocker = new object();
        private bool _running;
        private int _disposeCount;
        private bool _stopped;
        private int _started;
        //Guards the beat itself. A SemaphoreSlim rather than a lock because StopAsync and DisposeAsync
        //have to wait for an in-flight beat the same way Stop does, without parking the thread that is
        //finishing a message. Deliberately not disposed: nothing here touches AvailableWaitHandle, so
        //there is nothing to release, and disposing it would race a beat that is still on its way out.
        //Every wait on it passes CancellationToken.None on purpose. The only token in scope is _cancel,
        //which is what tells user code a beat has failed - waiting on it here would cancel the wait at
        //precisely the moment a beat is in trouble, and Stop would return while one was still updating.
        //That is the guarantee IHeartBeatWorker makes, so these waits are deliberately not cancellable,
        //exactly as the lock they replaced was not.
        private readonly SemaphoreSlim _beatLock = new SemaphoreSlim(1, 1);

        private readonly IHeartBeatScheduler _scheduler;

        private IScheduledJob _job;

        private readonly object _runningLock = new object();
        private readonly object _stoppedLock = new object();

        #endregion

        #region Private Methods

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="HeartBeatWorker" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="context">The context.</param>
        /// <param name="sendHeartBeat">The send heart beat.</param>
        /// <param name="scheduler">The scheduler.</param>
        /// <param name="log">The log.</param>
        /// <param name="heartBeatNotificationFactory">The heart beat notification factory.</param>
        public HeartBeatWorker(IHeartBeatConfiguration configuration,
            IMessageContext context,
            ISendHeartBeat sendHeartBeat,
            IHeartBeatScheduler scheduler,
            ILogger log,
            IWorkerHeartBeatNotificationFactory heartBeatNotificationFactory)
        {
            Guard.NotNull(configuration);
            Guard.NotNull(context);
            Guard.NotNull(sendHeartBeat);
            Guard.NotNull(scheduler);
            Guard.NotNull(log);
            Guard.NotNull(heartBeatNotificationFactory);

            _context = context;
            _checkTime = configuration.UpdateTime;
            _expiry = configuration.Time;
            _sendHeartbeat = sendHeartBeat;
            _scheduler = scheduler;
            _logger = log;

            _cancel = new CancellationTokenSource();
            context.WorkerNotification.HeartBeat = heartBeatNotificationFactory.Create(_cancel.Token);
        }

        #endregion

        /// <inheritdoc />
        public void Start()
        {
            ThrowIfDisposed();

            if (Interlocked.CompareExchange(ref _started, 1, 0) != 0)
            {
                throw new DotNetWorkQueueException("Start must only be called 1 time");
            }

            _beatLock.Wait(CancellationToken.None);
            try
            {
                if (!string.IsNullOrWhiteSpace(_checkTime))
                {
                    //the claim is fresh as of now; staleness is measured from here until a beat lands
                    Interlocked.Exchange(ref _lastGoodBeatUtcTicks, DateTime.UtcNow.Ticks);
                    _job = _scheduler.AddUpdateJob(
                        string.Concat("heartbeat-", _context.MessageId.ToString(), "-", Guid.NewGuid().ToString()), _checkTime, (message, notification) => SendHeartBeatInternal());
                }
            }
            finally
            {
                _beatLock.Release();
            }
        }

        /// <inheritdoc />
        public void Stop()
        {
            _beatLock.Wait(CancellationToken.None); //this will block if we are currently updating the heart beat
            try
            {
                Stopped = true;
            }
            finally
            {
                _beatLock.Release();
            }
        }

        /// <inheritdoc />
        public async Task StopAsync()
        {
            //same wait as Stop, awaited rather than blocked on
            await _beatLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            try
            {
                Stopped = true;
            }
            finally
            {
                _beatLock.Release();
            }
        }

        #region IDisposable

        /// <inheritdoc />
        public bool IsDisposed => Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0;

        /// <summary>
        /// Throws an exception if this instance has been disposed.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException"></exception>
        protected void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0, this);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (Interlocked.Increment(ref _disposeCount) != 1) return;

            _beatLock.Wait(CancellationToken.None);
            try
            {
                ReleaseSchedule();
            }
            finally
            {
                _beatLock.Release();
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Increment(ref _disposeCount) != 1) return;

            //the same teardown as Dispose, but the wait for an in-flight beat is awaited - this runs on
            //the asynchronous consumer's continuation, once per message
            await _beatLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            try
            {
                ReleaseSchedule();
            }
            finally
            {
                _beatLock.Release();
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Takes the job off the schedule and drops the cancel token. The caller holds the beat lock.
        /// </summary>
        private void ReleaseSchedule()
        {
            Stopped = true;
            _job?.StopSchedule();
            if (_job != null)
            {
                var removed = _scheduler?.RemoveJob(_job.Name);
                if (removed.HasValue && !removed.Value)
                {
                    _logger.LogWarning("Failed to remove job {JobName} from the heartbeat scheduler", _job.Name);
                }
            }
            lock (_cancelLocker)
            {
                if (_cancel == null) return;
                _cancel.Dispose();
                _cancel = null;
            }
        }
        #endregion

        /// <summary>
        /// Sends the heart beat.
        /// </summary>
        /// <remarks>
        /// The scheduler calls this on one of its own threads, and it hands that thread straight back:
        /// the beat continues on its own once the transport call yields. Nothing is lost by not waiting
        /// here - <see cref="StopAsync"/> and <see cref="DisposeAsync"/> wait on the beat lock, which is
        /// what actually bounds a beat's lifetime, and <see cref="SendHeartBeatAsync"/> handles its own
        /// failures, so the task it returns has nothing left to observe.
        /// </remarks>
        private void SendHeartBeatInternal()
        {
            //SendHeartBeatAsync handles a failing beat itself, but its own failure handling can throw -
            //a cancellation callback raising from SetCancel, say. Without this the task would fault with
            //nobody watching, and a heartbeat that silently stops beating is what lets the monitor reset
            //a message that is still being processed.
            _ = SendHeartBeatAsync().ContinueWith(
                t => _logger.LogError(t.Exception,
                    "An error has occurred while handling a failed heartbeat update"),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }

        /// <summary>
        /// Sends the heart beat.
        /// </summary>
        private async Task SendHeartBeatAsync()
        {
            await BeatOnceAsync().ConfigureAwait(false);

            //Take the slot that was lost while the beat above was running, rather than waiting for the
            //next tick. Bounded to one catch-up: the flag is cleared as it is read, and only a tick
            //arriving during a beat can set it again.
            if (Interlocked.Exchange(ref _missedTick, 0) == 1 && !IsDisposed && !Stopped)
            {
                await BeatOnceAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// One attempt at updating the heart beat.
        /// </summary>
        private async Task BeatOnceAsync()
        {
            if (IsDisposed)
                return;

            if (Stopped)
                return;

            //Checked on every tick, including the ones where the beat below is skipped - a beat that is
            //still running is exactly when the claim is most likely to be going stale.
            GuardAgainstAStaleClaim();

            if (Running)
            {
                //the previous beat has not come back yet; remember the slot so it is taken as soon as
                //that beat finishes instead of being given up
                Interlocked.Exchange(ref _missedTick, 1);
                _logger.LogWarning(
                    "Skipped a heartbeat for message {MessageId}: the previous update has not completed",
                    _context.MessageId?.Id?.Value);
                return;
            }

            try
            {
                await _beatLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);
                try
                {
                    if (IsDisposed)
                        return;

                    if (Running)
                        return;

                    if (Stopped)
                        return;

                    Running = true;
                    var status = await _sendHeartbeat.SendAsync(_context).ConfigureAwait(false);
                    if (status.LastHeartBeatTime.HasValue)
                    {
                        Interlocked.Exchange(ref _lastGoodBeatUtcTicks, DateTime.UtcNow.Ticks);
                        _context.WorkerNotification.HeartBeat.Status = status;
                        if (_logger.IsEnabled(LogLevel.Trace))
                            _logger.LogTrace("Set heartbeat for message {MessageId}", status.MessageId.Id.Value);
                    }
                    else
                    {
                        if (_logger.IsEnabled(LogLevel.Debug))
                            _logger.LogDebug(
                                "Failed to set heartbeat for message ID {MessageId}; since no exception was generated, this probably means that the record no longer exists", status.MessageId.Id.Value);
                    }
                }
                finally
                {
                    _beatLock.Release();
                }
            }
            catch (Exception error)
            {
                _logger.LogError(error,
                    "An error has occurred while updating the heartbeat field for a record that is being processed");

                await _beatLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);
                try
                {
                    _context.WorkerNotification.HeartBeat.SetError(error);
                }
                finally
                {
                    _beatLock.Release();
                }

                SetCancel();
            }
            finally
            {
                Running = false;
            }
        }

        /// <summary>
        /// Stops processing when this worker's claim on the message has aged out.
        /// </summary>
        /// <remarks>
        /// The monitor resets any message whose stored heartbeat is older than the expiry and hands it to
        /// another worker. Until this existed the original worker carried on and committed, so a lapsed
        /// heartbeat produced two workers finishing the same message rather than the redelivery it is
        /// supposed to produce. Cancelling here is the same signal a throwing beat already raised - user
        /// code is asked to stop, and the message is left for whoever holds the claim now.
        /// </remarks>
        private void GuardAgainstAStaleClaim()
        {
            if (_expiry <= TimeSpan.Zero)
                return; //no expiry configured, so nothing resets the message and the claim cannot lapse

            var last = new DateTime(Interlocked.Read(ref _lastGoodBeatUtcTicks), DateTimeKind.Utc);
            var age = DateTime.UtcNow - last;
            if (age < _expiry)
                return;

            _logger.LogError(
                "No heartbeat has been recorded for message {MessageId} in {Age}, which is past the {Expiry} the monitor resets against - the message may already have been given to another worker, so processing is being cancelled",
                _context.MessageId?.Id?.Value, age, _expiry);

            SetCancel();
        }

        /// <summary>
        /// Sets the cancel token to true
        /// </summary>
        private void SetCancel()
        {
            lock (_cancelLocker)
            {
                if (_cancel == null) return;

                if (!_cancel.IsCancellationRequested) //if we already set this, don't set it again
                {
                    //let the user code know that they may wish to abort processing, since the heart beat failed to update.
                    _cancel.Cancel();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="HeartBeatWorker"/> is running.
        /// </summary>
        /// <value>
        ///   <c>true</c> if running; otherwise, <c>false</c>.
        /// </value>
        private bool Running
        {
            get
            {
                if (IsDisposed)
                    return false;

                Monitor.Enter(_runningLock);
                try
                {
                    return _running;
                }
                finally
                {
                    Monitor.Exit(_runningLock);
                }
            }
            set
            {
                Monitor.Enter(_runningLock);
                try
                {
                    _running = value;
                }
                finally
                {
                    Monitor.Exit(_runningLock);
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="HeartBeatWorker"/> is stopped.
        /// </summary>
        /// <value>
        ///   <c>true</c> if stopped; otherwise, <c>false</c>.
        /// </value>
        private bool Stopped
        {
            get
            {
                if (IsDisposed)
                    return true;

                Monitor.Enter(_stoppedLock);
                try
                {
                    return _stopped;
                }
                finally
                {
                    Monitor.Exit(_stoppedLock);
                }
            }
            set
            {
                if (_stoppedLock == null)
                    return;

                Monitor.Enter(_stoppedLock);
                try
                {
                    _stopped = value;
                }
                finally
                {
                    Monitor.Exit(_stoppedLock);
                }
            }
        }
        #endregion
    }
}