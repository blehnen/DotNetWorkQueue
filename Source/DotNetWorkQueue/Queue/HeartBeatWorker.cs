// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using System.Runtime.CompilerServices;
using System.Threading;
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
        private readonly ISendHeartBeat _sendHeartbeat;
        private readonly IMessageContext _context;
        private CancellationTokenSource _cancel;
        private readonly object _cancelLocker = new object();
        private bool _running;
        private int _disposeCount;
        private bool _stopped;
        private volatile bool _started;
        private readonly object _runningLocker = new object();

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
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => context, context);
            Guard.NotNull(() => sendHeartBeat, sendHeartBeat);
            Guard.NotNull(() => scheduler, scheduler);
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => heartBeatNotificationFactory, heartBeatNotificationFactory);

            _context = context;
            _checkTime = configuration.UpdateTime;
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

            if (_started)
            {
                throw new DotNetWorkQueueException("Start must only be called 1 time");
            }
            _started = true;

            lock (_runningLocker)
            {
                if (!string.IsNullOrWhiteSpace(_checkTime))
                {
                    _job = _scheduler.AddUpdateJob(
                        string.Concat("heartbeat-", _context.MessageId.ToString(), "-", Guid.NewGuid().ToString()), _checkTime, (message, notification) => SendHeartBeatInternal());
                }
            }
        }

        /// <inheritdoc />
        public void Stop()
        {
            lock (_runningLocker) //this will block if we are currently updating the heart beat
            {
                Stopped = true;
            }
        }

        #region IDisposable

        /// <inheritdoc />
        public bool IsDisposed => Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0;

        /// <summary>
        /// Throws an exception if this instance has been disposed.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <exception cref="System.ObjectDisposedException"></exception>
        protected void ThrowIfDisposed([CallerMemberName] string name = "")
        {
            if (Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0)
            {
                throw new ObjectDisposedException(name);
            }
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

            lock (_runningLocker)
            {
                Stopped = true;
                _job?.StopSchedule();
                if (_job != null)
                {
                    var removed = _scheduler?.RemoveJob(_job.Name);
                    if (removed.HasValue && !removed.Value)
                    {
                        _logger.LogWarning($"Failed to remove job {_job.Name} from the heartbeat scheduler");
                    }
                }
                lock (_cancelLocker)
                {
                    if (_cancel == null) return;
                    _cancel.Dispose();
                    _cancel = null;
                }
            }
        }
        #endregion

        /// <summary>
        /// Sends the heart beat.
        /// </summary>
        private void SendHeartBeatInternal()
        {
            if (IsDisposed)
                return;

            if (Running)
                return;

            if (Stopped)
                return;

            try
            {
                lock (_runningLocker)
                {
                    if (IsDisposed)
                        return;

                    if (Running)
                        return;

                    if (Stopped)
                        return;

                    Running = true;
                    var status = _sendHeartbeat.Send(_context);
                    if (status.LastHeartBeatTime.HasValue)
                    {
                        _context.WorkerNotification.HeartBeat.Status = status;
                        _logger.LogTrace($"Set heartbeat for message {status.MessageId.Id.Value}");
                    }
                    else
                    {
                        _logger.LogDebug(
                            $"Failed to set heartbeat for message ID {status.MessageId.Id.Value}; since no exception was generated, this probably means that the record no longer exists");
                    }
                }
            }
            // ReSharper disable once UncatchableException
            catch (ThreadAbortException error)
            {
                _logger.LogWarning(
                    "The worker thread has been aborted");

                lock (_runningLocker)
                {
                    _context.WorkerNotification.HeartBeat.SetError(error);
                }

                SetCancel();
            }
            catch (Exception error)
            {
                _logger.LogError(
                    $"An error has occurred while updating the heartbeat field for a record that is being processed{System.Environment.NewLine}{error}");

                lock (_runningLocker)
                {
                    _context.WorkerNotification.HeartBeat.SetError(error);
                }

                SetCancel();
            }
            finally
            {
                Running = false;
            }
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