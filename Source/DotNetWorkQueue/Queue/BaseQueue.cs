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
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Base queue class
    /// </summary>
    public abstract class BaseQueue : IDisposable, IIsDisposed
    {
        private bool _started;
        private bool _shouldWork;

        private readonly object _shouldWorkLocker = new object();
        private readonly object _startedLocker = new object();

        private int _disposeCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseQueue" /> class.
        /// </summary>
        /// <param name="log">The log.</param>
        protected BaseQueue(ILogger log)
        {
            Guard.NotNull(() => log, log);

            Log = log;
        }
        /// <summary>
        /// Gets or sets the log.
        /// </summary>
        /// <value>
        /// The log.
        /// </value>
        protected ILogger Log { get; }
        /// <summary>
        /// Logs the system exception.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="WorkerErrorEventArgs"/> instance containing the event data.</param>
        protected void LogSystemException(object sender, WorkerErrorEventArgs e)
        {
            Log.LogError($"Unhanded system exception{System.Environment.NewLine}{e.Error}");
        }
        /// <summary>
        /// Logs the user exception.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="WorkerErrorEventArgs"/> instance containing the event data.</param>
        protected void LogUserException(object sender, WorkerErrorEventArgs e)
        {
            Log.LogError($"User exception{System.Environment.NewLine}{e.Error}");
        }
        /// <summary>
        /// Gets or sets a value indicating whether work can proceed.
        /// </summary>
        /// <remarks>If this is false, no new work should be started.</remarks>
        /// <value>
        ///   <c>true</c> if [should work]; otherwise, <c>false</c>.
        /// </value>
        protected bool ShouldWork
        {
            get
            {
                if (IsDisposed)
                    return false;

                Monitor.Enter(_shouldWorkLocker);
                try
                {
                    return _shouldWork;
                }
                finally
                {
                    Monitor.Exit(_shouldWorkLocker);
                }
            }
            set
            {
                ThrowIfDisposed();

                Monitor.Enter(_shouldWorkLocker);
                try
                {
                    _shouldWork = value;
                }
                finally
                {
                    Monitor.Exit(_shouldWorkLocker);
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="BaseQueue"/> is started.
        /// </summary>
        /// <value>
        ///   <c>true</c> if started; otherwise, <c>false</c>.
        /// </value>
        public bool Started
        {
            get
            {
                if (IsDisposed)
                    return false;

                Monitor.Enter(_startedLocker);
                try
                {
                    return _started;
                }
                finally
                {
                    Monitor.Exit(_startedLocker);
                }
            }
            set
            {
                ThrowIfDisposed();

                Monitor.Enter(_startedLocker);
                try
                {
                    _started = value;
                }
                finally
                {
                    Monitor.Exit(_startedLocker);
                }
            }
        }

        #region IDispose, IIsDisposed
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
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            Interlocked.Increment(ref _disposeCount);
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
