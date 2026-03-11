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
using System.Runtime.CompilerServices;
using System.Threading;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Runs queue maintenance tasks independently of consumer message processing.
    /// Wraps the transport's <see cref="IQueueMonitor"/> (which includes any transport-specific monitors).
    /// </summary>
    public class QueueMaintenanceService : IQueueMaintenanceService
    {
        private readonly IQueueMonitor _queueMonitor;
        private readonly ILogger _log;
        private int _disposeCount;
        private bool _started;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueMaintenanceService"/> class.
        /// </summary>
        /// <param name="queueMonitor">The transport's queue monitor.</param>
        /// <param name="log">The logger.</param>
        public QueueMaintenanceService(IQueueMonitor queueMonitor, ILogger log)
        {
            Guard.NotNull(() => queueMonitor, queueMonitor);
            Guard.NotNull(() => log, log);

            _queueMonitor = queueMonitor;
            _log = log;
        }

        /// <inheritdoc />
        public void Start()
        {
            ThrowIfDisposed();

            if (_started)
                return;

            _started = true;
            _queueMonitor.Start();
            _log.LogInformation("Queue maintenance service started");
        }

        /// <inheritdoc />
        public void Stop()
        {
            ThrowIfDisposed();

            if (!_started)
                return;

            _queueMonitor.Stop();
            _started = false;
            _log.LogInformation("Queue maintenance service stopped");
        }

        /// <inheritdoc />
        public bool IsRunning => _started && !IsDisposed;

        /// <inheritdoc />
        public DateTime? LastRun => null; // TODO: wire up when BaseMonitor exposes last-run timestamps

        #region IDisposable, IIsDisposed

        /// <summary>
        /// Throws an exception if this instance has been disposed.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <exception cref="System.ObjectDisposedException"></exception>
        private void ThrowIfDisposed([CallerMemberName] string name = "")
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

            if (_started && !_queueMonitor.IsDisposed)
            {
                _queueMonitor.Stop();
                _started = false;
            }

            // Do NOT dispose _queueMonitor here — it's a shared singleton
            // managed by the container's lifecycle.
        }

        /// <inheritdoc />
        public bool IsDisposed => Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0;

        #endregion
    }
}
