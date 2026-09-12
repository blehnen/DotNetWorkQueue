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
        private readonly TimeSpan _interval;
        //The window the monitor resets against. A message whose stored heartbeat is older than this is
        //treated as abandoned and handed to another worker, so it is also the deadline this worker has
        //to keep meeting to keep its claim.
        private readonly TimeSpan _expiry;
        //Ticks of the last beat that actually landed. Long, not DateTime, so it can be read and written
        //without the beat lock - the staleness check runs on ticks where the beat itself is skipped.
        private long _lastGoodBeatUtcTicks;
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

        //One timer per worker, in place of a cron job on a shared scheduler. PeriodicTimer is the
        //point of this change: when a tick is missed because the previous beat ran long, the next
        //WaitForNextTickAsync returns straight away rather than waiting for the following boundary. It
        //fires late; it does not skip. Losing whole slots is what cost a worker its claim.
        private PeriodicTimer _timer;
        private Task _loop;
        private readonly CancellationTokenSource _loopCancel = new CancellationTokenSource();
        //Bounds beats in flight across the consumer. A beat waits for a slot; it is never dropped.
        private readonly IHeartBeatGate _gate;
        //How long teardown waits for a beat that is already on its way out.
        private readonly TimeSpan _drainTimeout;
        //The transport's clock, not the machine's. The monitor ages the stored heartbeat against this
        //same source - on Redis it is the Redis server's time - so comparing against _getTime.GetCurrentUtcDate()
        //here would measure the claim against a different clock than the one that reclaims it.
        private readonly IGetTime _getTime;


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
        /// <param name="gate">Bounds how many beats the consumer has in flight at once.</param>
        /// <param name="log">The log.</param>
        /// <param name="heartBeatNotificationFactory">The heart beat notification factory.</param>
        /// <param name="getTimeFactory">The time factory; the transport's clock, which is the one the monitor ages against.</param>
        public HeartBeatWorker(IHeartBeatConfiguration configuration,
            IMessageContext context,
            ISendHeartBeat sendHeartBeat,
            IHeartBeatGate gate,
            ILogger log,
            IWorkerHeartBeatNotificationFactory heartBeatNotificationFactory,
            IGetTimeFactory getTimeFactory)
        {
            Guard.NotNull(configuration);
            Guard.NotNull(getTimeFactory);
            Guard.NotNull(context);
            Guard.NotNull(sendHeartBeat);
            Guard.NotNull(gate);
            Guard.NotNull(log);
            Guard.NotNull(heartBeatNotificationFactory);

            _context = context;
            _interval = configuration.UpdateTime;
            _expiry = configuration.Time;
            _drainTimeout = configuration.ThreadPoolConfiguration.WaitForThreadPoolToFinish;
            _sendHeartbeat = sendHeartBeat;
            _gate = gate;
            _getTime = getTimeFactory.Create();
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
                if (_interval > TimeSpan.Zero)
                {
                    //the claim is fresh as of now; staleness is measured from here until a beat lands
                    Interlocked.Exchange(ref _lastGoodBeatUtcTicks, _getTime.GetCurrentUtcDate().Ticks);
                    _timer = new PeriodicTimer(_interval);
                    _loop = Task.Run(RunLoopAsync, CancellationToken.None);
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

            //outside the beat lock on purpose: the loop takes that same lock, so waiting for it while
            //holding it would deadlock
            StopLoop();

            //bounded for the same reason the loop wait is: a transport call that never returns holds
            //this lock, and an unbounded wait here would block the message from completing at all
            var taken = _beatLock.Wait(DrainWindow(), CancellationToken.None);
            try
            {
                if (!taken)
                    _logger.LogWarning("A heartbeat was still updating after {Timeout}; tearing down anyway", DrainWindow());
                ReleaseCancel();
            }
            finally
            {
                if (taken) _beatLock.Release();
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Increment(ref _disposeCount) != 1) return;

            await StopLoopAsync().ConfigureAwait(false);

            //the same teardown as Dispose, but the wait for an in-flight beat is awaited - this runs on
            //the asynchronous consumer's continuation, once per message. Bounded for the same reason.
            var taken = await _beatLock.WaitAsync(DrainWindow(), CancellationToken.None).ConfigureAwait(false);
            try
            {
                if (!taken)
                    _logger.LogWarning("A heartbeat was still updating after {Timeout}; tearing down anyway", DrainWindow());
                ReleaseCancel();
            }
            finally
            {
                if (taken) _beatLock.Release();
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Stops the timer and waits for a beat that is already running, bounded by
        /// <see cref="IHeartBeatThreadPoolConfiguration.WaitForThreadPoolToFinish"/>.
        /// </summary>
        /// <remarks>Must not be called while holding the beat lock - the loop takes it.</remarks>
        private void StopLoop()
        {
            var loop = SignalLoopToStop();
            if (loop == null) return;
            try
            {
                //a beat that has not come back inside the drain window is abandoned rather than holding
                //up the consumer's shutdown; the transport call will finish or fault on its own
                if (!loop.Wait(DrainWindow(), CancellationToken.None))
                    _logger.LogWarning("A heartbeat was still running after {Timeout}; it was not waited for", DrainWindow());
            }
            catch (AggregateException error)
            {
                _logger.LogError(error, "The heartbeat loop faulted while shutting down");
            }
        }

        /// <summary>Awaited form of <see cref="StopLoop"/>.</summary>
        private async Task StopLoopAsync()
        {
            var loop = SignalLoopToStop();
            if (loop == null) return;
            try
            {
                var finished = await Task.WhenAny(loop, Task.Delay(DrainWindow(), CancellationToken.None)).ConfigureAwait(false);
                if (!ReferenceEquals(finished, loop))
                    _logger.LogWarning("A heartbeat was still running after {Timeout}; it was not waited for", DrainWindow());
                else
                    await loop.ConfigureAwait(false);
            }
            catch (Exception error)
            {
                _logger.LogError(error, "The heartbeat loop faulted while shutting down");
            }
        }

        private TimeSpan DrainWindow() =>
            _drainTimeout > TimeSpan.Zero ? _drainTimeout : TimeSpan.FromSeconds(5);

        private Task SignalLoopToStop()
        {
            Stopped = true;
            try
            {
                _loopCancel.Cancel();
            }
            catch (ObjectDisposedException)
            {
                //already torn down
            }
            _timer?.Dispose();
            return _loop;
        }

        /// <summary>
        /// Drops the cancel token. The caller holds the beat lock.
        /// </summary>
        private void ReleaseCancel()
        {
            Stopped = true;
            //disposed here rather than in SignalLoopToStop: the loop is awaiting on this token, and
            //disposing it while that wait is unwinding races the cancellation
            _loopCancel.Dispose();
            lock (_cancelLocker)
            {
                if (_cancel == null) return;
                _cancel.Dispose();
                _cancel = null;
            }
        }
        #endregion

        /// <summary>
        /// Beats on the interval until the worker is stopped or disposed.
        /// </summary>
        /// <remarks>
        /// Serial by construction: the next tick is not awaited until the current beat has returned, so
        /// two beats for one message cannot overlap. A beat that runs longer than the interval delays
        /// the following one rather than forfeiting its slot.
        /// </remarks>
        private async Task RunLoopAsync()
        {
            try
            {
                while (await _timer.WaitForNextTickAsync(_loopCancel.Token).ConfigureAwait(false))
                {
                    if (IsDisposed || Stopped)
                        return;

                    //bounded by the interval: a beat held back until the claim has lapsed would make
                    //this worker cancel itself over a queue the gate created
                    using (await _gate.EnterAsync(_interval, _loopCancel.Token).ConfigureAwait(false))
                    {
                        await BeatOnceAsync().ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                //teardown
            }
            catch (ObjectDisposedException)
            {
                //the timer or the gate went away under us during teardown
            }
            catch (Exception error)
            {
                //BeatOnceAsync handles a failing beat itself, so reaching here means its own failure
                //handling threw. Letting the loop die silently is what lets the monitor reset a message
                //that is still being processed, so it is logged rather than swallowed.
                _logger.LogError(error, "The heartbeat loop has stopped unexpectedly");
            }
        }

        /// <summary>
        /// One attempt at updating the heart beat.
        /// </summary>
        /// <remarks>
        /// Internal so tests can drive a beat without waiting on the timer. The loop is the only
        /// production caller.
        /// </remarks>
        internal async Task BeatOnceAsync()
        {
            if (IsDisposed)
                return;

            if (Stopped)
                return;

            //Checked on every tick, including the ones where the beat below is skipped - a beat that is
            //still running is exactly when the claim is most likely to be going stale.
            if (ClaimHasLapsed() && await ConfirmLapsedAgainstAnyBeatStillRunning().ConfigureAwait(false))
            {
                //do not beat: the update would match the row again and renew a claim that has already
                //been declared lost, putting this worker back to looking alive while it is stopping
                return;
            }

            if (Running)
            {
                //The loop cannot reach here - it does not ask for the next tick until this beat has
                //returned. It remains as a guard for anything that drives a beat directly.
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
                    //Freshness is measured from when the update was sent, not from when the reply came
                    //back. A slow reply would otherwise start a new local window while the record the
                    //monitor ages was already older than that - this errs on the side of believing the
                    //claim is staler than it is, which is the safe direction. Local clock only: the
                    //stored timestamp comes from the transport's clock and is not comparable here.
                    var sentAt = _getTime.GetCurrentUtcDate();
                    var status = await _sendHeartbeat.SendAsync(_context).ConfigureAwait(false);
                    if (status.LastHeartBeatTime.HasValue)
                    {
                        Interlocked.Exchange(ref _lastGoodBeatUtcTicks, sentAt.Ticks);
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
        private bool ClaimHasLapsed()
        {
            if (_expiry <= TimeSpan.Zero)
                return false; //no expiry configured, so nothing resets the message and the claim cannot lapse

            var last = new DateTime(Interlocked.Read(ref _lastGoodBeatUtcTicks), DateTimeKind.Utc);
            var age = _getTime.GetCurrentUtcDate() - last;
            if (age < _expiry)
                return false;

            return true;
        }

        /// <summary>
        /// Confirms a lapse against any beat that is still in flight, and cancels if it is real.
        /// </summary>
        /// <remarks>
        /// A beat can have succeeded at the transport and not yet published its timestamp, because the
        /// scheduler starts a tick without waiting for the previous beat. Reading the claim's age in that
        /// window sees the old value and would cancel a worker whose claim had just been renewed - a
        /// false positive that aborts healthy work. Waiting on the beat lock orders this after any beat
        /// that is mid-flight, so the second look sees whatever that beat published.
        /// </remarks>
        private async Task<bool> ConfirmLapsedAgainstAnyBeatStillRunning()
        {
            await _beatLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            try
            {
                if (!ClaimHasLapsed())
                    return false; //a beat landed while we were waiting; the claim is good
            }
            finally
            {
                _beatLock.Release();
            }

            var age = _getTime.GetCurrentUtcDate() -
                      new DateTime(Interlocked.Read(ref _lastGoodBeatUtcTicks), DateTimeKind.Utc);
            _logger.LogError(
                "No heartbeat has been recorded for message {MessageId} in {Age}, which is past the {Expiry} the monitor resets against - the message may already have been given to another worker, so processing is being cancelled",
                _context.MessageId?.Id?.Value, age, _expiry);

            SetCancel();
            return true;
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