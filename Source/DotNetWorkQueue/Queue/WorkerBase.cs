// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// The base worker class that the actual workers inherit from
    /// </summary>
    internal abstract class WorkerBase : IWorkerBase
    {
        private readonly WorkerTerminate _workerTerminate;
        private readonly object _shouldExitLock = new object();

        private bool _shouldExit;
        protected Thread WorkerThread;

        private int _disposeCount;

        /// <summary>
        /// Event that will be raised each time message delivery fails.
        /// </summary>
        public event EventHandler<WorkerErrorEventArgs> UserException;

        /// <summary>
        /// Event that will be raised if an exception occurs outside of user code.
        /// </summary>
        public event EventHandler<WorkerErrorEventArgs> SystemException;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerBase"/> class.
        /// </summary>
        /// <param name="workerTerminate">The worker terminate.</param>
        protected WorkerBase(WorkerTerminate workerTerminate)
        {
            Guard.NotNull(() => workerTerminate, workerTerminate);
            _workerTerminate = workerTerminate;
        }
        /// <summary>
        /// Raises the system exception.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="WorkerErrorEventArgs"/> instance containing the event data.</param>
        protected void RaiseSystemException(object sender, WorkerErrorEventArgs e)
        {
            SystemException?.Invoke(sender, e);
        }

        /// <summary>
        /// Raises the user exception.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="WorkerErrorEventArgs"/> instance containing the event data.</param>
        protected void RaiseUserException(object sender, WorkerErrorEventArgs e)
        {
            UserException?.Invoke(sender, e);
        }

        /// <summary>
        /// Raises the system exception.
        /// </summary>
        /// <param name="error">The error.</param>
        protected void RaiseSystemException(Exception error)
        {
            RaiseSystemException(this, new WorkerErrorEventArgs(this, error));
        }
        /// <summary>
        /// Raises the user exception.
        /// </summary>
        /// <param name="error">The error.</param>
        protected void RaiseUserException(Exception error)
        {
            RaiseUserException(this, new WorkerErrorEventArgs(this, error));
        }

        /// <summary>
        /// Raises the system message exception.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="MessageErrorEventArgs"/> instance containing the event data.</param>
        protected void RaiseSystemMessageException(object sender, MessageErrorEventArgs e)
        {
            RaiseSystemException(this, new WorkerErrorEventArgs(this, e.Error));
        }

        /// <summary>
        /// Raises the user message exception.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="MessageErrorEventArgs"/> instance containing the event data.</param>
        protected void RaiseUserMessageException(object sender, MessageErrorEventArgs e)
        {
            RaiseUserException(this, new WorkerErrorEventArgs(this, e.Error));
        }

        /// <summary>
        /// Gets or sets a value indicating whether [should exit].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [should exit]; otherwise, <c>false</c>.
        /// </value>
        protected bool ShouldExit
        {
            get
            {
                if (IsDisposed)
                    return true;

                Monitor.Enter(_shouldExitLock);
                try
                {
                    return _shouldExit;
                }
                finally
                {
                    Monitor.Exit(_shouldExitLock);
                }
            }
            set
            {
                if (IsDisposed)
                    return;

                Monitor.Enter(_shouldExitLock);
                try
                {
                    _shouldExit = value;
                }
                finally
                {
                    Monitor.Exit(_shouldExitLock);
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
        public virtual void Dispose()
        {
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

        /// <summary>
        /// Attempts to terminate the worker thread.
        /// </summary>
        /// <returns></returns>
        public virtual bool AttemptToTerminate()
        {
            return _workerTerminate.AttemptToTerminate(WorkerThread, TimeSpan.Zero);
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="IWorkerBase" /> is running.
        /// </summary>
        /// <value>
        ///   <c>true</c> if running; otherwise, <c>false</c>.
        /// </value>
        public abstract bool Running { get; }
        /// <summary>
        /// Starts this instance.
        /// </summary>
        public abstract void Start();
        /// <summary>
        /// Stops this instance.
        /// </summary>
        public abstract void Stop();
        /// <summary>
        /// Forces the worker to terminate.
        /// <remarks>This method should not return until the worker has shutdown.</remarks>
        /// </summary>
        public abstract void TryForceTerminate();
    }
}
