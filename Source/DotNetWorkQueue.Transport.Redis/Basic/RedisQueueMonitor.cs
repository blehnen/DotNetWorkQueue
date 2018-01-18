using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// An overridden queue monitor that allows us to inject our delayed processing monitor
    /// </summary>
    internal class RedisQueueMonitor : IQueueMonitor
    {
        private readonly List<IMonitor> _monitors;
        private readonly IClearExpiredMessagesMonitor _clearMessagesFactory;
        private readonly IHeartBeatMonitor _heartBeatFactory;
        private readonly IDelayedProcessingMonitor _delayedProcessing;
        private int _disposeCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueMonitor" /> class.
        /// </summary>
        /// <param name="clearMessagesFactory">The clear messages factory.</param>
        /// <param name="heartBeatFactory">The heart beat factory.</param>
        /// <param name="delayedProcessing">The delayed processing.</param>
        public RedisQueueMonitor(IClearExpiredMessagesMonitor clearMessagesFactory,
            IHeartBeatMonitor heartBeatFactory,
            IDelayedProcessingMonitor delayedProcessing)
        {
            Guard.NotNull(() => clearMessagesFactory, clearMessagesFactory);
            Guard.NotNull(() => heartBeatFactory, heartBeatFactory);
            _heartBeatFactory = heartBeatFactory;
            _clearMessagesFactory = clearMessagesFactory;
            _delayedProcessing = delayedProcessing;
            _monitors = new List<IMonitor>(3);
        }
        /// <summary>
        /// Starts the monitor process.
        /// </summary>
        public void Start()
        {
            ThrowIfDisposed();

            if (_monitors.Count > 0)
            {
                throw new DotNetWorkQueueException("Start must only be called 1 time");    
            }

            _monitors.Add(_heartBeatFactory);
            _monitors.Add(_clearMessagesFactory);
            _monitors.Add(_delayedProcessing);
            _monitors.AsParallel().ForAll(w => w.Start());
        }

        /// <summary>
        /// Stops the monitor process.
        /// </summary>
        public void Stop()
        {
            ThrowIfDisposed();
            _monitors.AsParallel().ForAll(w => w.Stop());
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

            if (Interlocked.Increment(ref _disposeCount) != 1) return;
            _monitors.AsParallel().ForAll(w => w.Dispose());
            _monitors.Clear();
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
