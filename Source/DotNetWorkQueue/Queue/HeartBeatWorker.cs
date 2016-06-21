// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// A class that will update the heart beat field for a work item
    /// </summary>
    public class HeartBeatWorker : IHeartBeatWorker
    {
        #region Member level Variables

        private readonly ILog _logger;
        private Timer _timer;
        private readonly TimeSpan _checkTimespan;
        private readonly ISendHeartBeat _sendHeartbeat;
        private readonly IMessageContext _context;
        private CancellationTokenSource _cancel;
        private readonly object _cancelLocker = new object();
        private bool _running;
        private int _disposeCount;
        private bool _stopped;
        private volatile bool _started;
        private readonly object _runningLocker = new object();
        private readonly IHeartBeatThreadPool _smartThreadPool;

        private ReaderWriterLockSlim _runningLock;
        private ReaderWriterLockSlim _stoppedLock;

        #endregion

        #region Private Methods

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="HeartBeatWorker" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="context">The context.</param>
        /// <param name="sendHeartBeat">The send heart beat.</param>
        /// <param name="threadPool">The thread pool.</param>
        /// <param name="log">The log.</param>
        /// <param name="heartBeatNotificationFactory">The heart beat notification factory.</param>
        public HeartBeatWorker(IHeartBeatConfiguration configuration, 
            IMessageContext context,
            ISendHeartBeat sendHeartBeat,
            IHeartBeatThreadPool threadPool,
            ILogFactory log,
            IWorkerHeartBeatNotificationFactory heartBeatNotificationFactory)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => context, context);
            Guard.NotNull(() => sendHeartBeat, sendHeartBeat);
            Guard.NotNull(() => threadPool, threadPool);
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => heartBeatNotificationFactory, heartBeatNotificationFactory);

            _context = context;
            _checkTimespan = configuration.CheckTime;
            _sendHeartbeat = sendHeartBeat;
            _smartThreadPool = threadPool;
            _logger = log.Create();

            _runningLock = new ReaderWriterLockSlim();
            _stoppedLock = new ReaderWriterLockSlim();

            _cancel = new CancellationTokenSource();
            context.WorkerNotification.HeartBeat = heartBeatNotificationFactory.Create(_cancel.Token);
        }

        #endregion

        /// <summary>
        /// Starts this instance.
        /// </summary>
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
                if (_checkTimespan.TotalSeconds > 0)
                {
                    _timer = new Timer(_ => SendHeartBeat(), null, _checkTimespan, Timeout.InfiniteTimeSpan);
                }
            }
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        /// <remarks>
        /// Stop is explicitly called when an error occurs, so that we can preserve the last heartbeat value.
        /// Implementations MUST ensure that stop blocks and does not return if the heartbeat is in the middle of updating.
        /// </remarks>
        public void Stop()
        {
            lock (_runningLocker) //this will block if we are currently updating the heart beat
            {
                Stopped = true;
            }
        }

        #region IDisposable

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_timer", Justification = "not needed")]
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (Interlocked.Increment(ref _disposeCount) != 1) return;

            lock (_runningLocker)
            {
                Stopped = true;
                _timer?.Dispose();
                if (_cancel != null)
                {
                    lock (_cancelLocker)
                    {
                        if (_cancel != null)
                        {
                            _cancel.Dispose();
                            _cancel = null;
                        }
                    }
                }

                _runningLock.Dispose();
                _stoppedLock.Dispose();
                _runningLock = null;
                _stoppedLock = null;
            }
        }
        #endregion

        /// <summary>
        /// Sends the heart beat.
        /// </summary>
        private void SendHeartBeat()
        {
            if (IsDisposed)
                return;

            if (Running)
                return;

            if (Stopped)
                return;

            if (!_smartThreadPool.IsShuttingdown && !_smartThreadPool.IsDisposed)
            {
                _smartThreadPool.QueueWorkItem(SendHeartBeatInternal);
            }
        }

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
                        _logger.TraceFormat("Set heartbeat for message {0}", status.MessageId.Id.Value);
                    }
                    else
                    {
                        _logger.DebugFormat(
                            "Failed to set heartbeat for message ID {0}; since no exception was generated, this probably means that the record no longer exists",
                            status.MessageId.Id.Value);
                    }
                }
            }
            // ReSharper disable once UncatchableException
            catch (ThreadAbortException error)
            {
                if (_logger.IsWarnEnabled())
                {
                    _logger.WarnException(
                        "The worker thread has been aborted",
                        error);
                }
                _context.WorkerNotification.HeartBeat.SetError(error);
                SetCancel();
            }
            catch (Exception error)
            {
                if (_logger.IsErrorEnabled())
                {
                    _logger.ErrorException(
                        "An error has occurred while updating the heartbeat field for a record that is being processed",
                        error);
                }
                _context.WorkerNotification.HeartBeat.SetError(error);
                SetCancel();
            }
            finally
            {
                Running = false;
            }

            if (!IsDisposed && !Stopped)
            {
                _timer.Change(_checkTimespan, Timeout.InfiniteTimeSpan);
            }
        }

        /// <summary>
        /// Sets the cancel token to true
        /// </summary>
        private void SetCancel()
        {
            if (_cancel == null) return;

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

                _runningLock.EnterReadLock();
                try
                {
                    return _running;
                }
                finally
                {
                    _runningLock.ExitReadLock();
                }
            }
            set
            {
                if (_runningLock == null)
                    return;

                _runningLock.EnterWriteLock();
                try
                {
                    _running = value;
                }
                finally
                {
                    _runningLock.ExitWriteLock();
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

                _stoppedLock.EnterReadLock();
                try
                {
                    return _stopped;
                }
                finally
                {
                    _stoppedLock.ExitReadLock();
                }
            }
            set
            {
                if (_stoppedLock == null)
                    return;

                _stoppedLock.EnterWriteLock();
                try
                {
                    _stopped = value;
                }
                finally
                {
                    _stoppedLock.ExitWriteLock();
                }
            }
        }
        #endregion
    }
}