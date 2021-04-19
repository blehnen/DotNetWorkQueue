// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// A base monitor class for running actions on a queue
    /// </summary>
    public abstract class BaseMonitor: IMonitor
    {
        private readonly Func<CancellationToken, long> _monitorAction;
        private readonly Func<CancellationToken, List<ResetHeartBeatOutput>> _monitorActionIds;
        private readonly IMonitorTimespan _monitorTimeSpan;

        private Timer _timer;
        private CancellationTokenSource _cancel;
        private bool _running;
        private volatile bool _stopping;
        private readonly object _runningLock = new object();
        private readonly ILogger _log;
        private readonly object _cancelSync = new object();
        private int _disposeCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseMonitor" /> class.
        /// </summary>
        /// <param name="monitorAction">The monitor action.</param>
        /// <param name="monitorTimeSpan">The monitor time span.</param>
        /// <param name="log">The log.</param>
        protected BaseMonitor(Func<CancellationToken, long> monitorAction, 
            IMonitorTimespan monitorTimeSpan,
            ILogger log)
        {
            Guard.NotNull(() => monitorAction, monitorAction);
            Guard.NotNull(() => monitorTimeSpan, monitorTimeSpan);
            Guard.NotNull(() => log, log);

            _monitorAction = monitorAction;
            _monitorActionIds = null;
            _monitorTimeSpan = monitorTimeSpan;
            _log = log;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseMonitor"/> class.
        /// </summary>
        /// <param name="monitorAction">The monitor action.</param>
        /// <param name="monitorTimeSpan">The monitor time span.</param>
        /// <param name="log">The log.</param>
        protected BaseMonitor(IMonitorTimespan monitorTimeSpan, 
            Func<CancellationToken, List<ResetHeartBeatOutput>> monitorAction,
            ILogger log)
        {
            Guard.NotNull(() => monitorAction, monitorAction);
            Guard.NotNull(() => monitorTimeSpan, monitorTimeSpan);
            Guard.NotNull(() => log, log);

            _monitorActionIds = monitorAction;
            _monitorAction = null;
            _monitorTimeSpan = monitorTimeSpan;
            _log = log;
        }
        /// <summary>
        /// Starts the monitor process.
        /// </summary>
        public void Start()
        {
            _stopping = false;
            if (_monitorTimeSpan.MonitorTime.TotalMilliseconds > 0)
            {
                _timer = new Timer(_ => RunMonitor(), null, 0, Timeout.Infinite);
            }
        }

        /// <summary>
        /// Stops the monitor process.
        /// </summary>
        public void Stop()
        {
            _stopping = true;
            Cancel();
        }

        #region Private Methods
        /// <summary>
        /// Fires the monitor action
        /// </summary>
        private void RunMonitor()
        {
            if (!_stopping)
            {
                try
                {
                    Running = true;
                    CancelToken = new CancellationTokenSource();
                    if (_monitorAction != null)
                        _monitorAction(CancelToken.Token);
                    else
                        _monitorActionIds(CancelToken.Token);
                }
                catch (Exception error)
                {
                    _log.LogError("An exception has occurred in the monitor delegate", error);
                }
                finally
                {
                    CancelTokenDestroy();
                    Running = false;
                }

                if (!_stopping && _timer != null && _monitorTimeSpan != null)
                {
                    _timer.Change(_monitorTimeSpan.MonitorTime, Timeout.InfiniteTimeSpan);
                }
            }
        }

        /// <summary>
        /// Destroys the cancel token
        /// </summary>
        private void CancelTokenDestroy()
        {
            lock (_cancelSync)
            {
                if (CancelToken == null) return;
                CancelToken.Dispose();
                CancelToken = null;
            }
        }
        /// <summary>
        /// Cancels this instance.
        /// </summary>
        private void Cancel()
        {
            lock (_cancelSync)
            {
                CancelToken?.Cancel();
            }

            //wait for the current process to finish. It should be respecting the cancel
            //token, so it should not take long.
            while (Running)
            {
                Thread.Sleep(20);
            }

            CancelTokenDestroy();
        }
        /// <summary>
        /// Gets or sets the cancel token.
        /// </summary>
        /// <value>
        /// The cancel token.
        /// </value>
        private CancellationTokenSource CancelToken
        {
            get
            {
                lock (_cancelSync)
                {
                    return _cancel;
                }
            }
            set
            {
                lock (_cancelSync)
                {
                    _cancel = value;
                }
            }
        }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="BaseMonitor"/> is running.
        /// </summary>
        /// <value>
        ///   <c>true</c> if running; otherwise, <c>false</c>.
        /// </value>
        protected bool Running
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

                ThrowIfDisposed();
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
        #endregion

        #region IDisposable, IsDisposed
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
        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_timer", Justification = "not needed")]
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (Running)
            {
                Stop();
            }

            if (Interlocked.Increment(ref _disposeCount) != 1) return;

            _timer?.Dispose();
        }

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed => Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0;

        #endregion
    }
}
