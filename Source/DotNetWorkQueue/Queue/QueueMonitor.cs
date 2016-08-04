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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Wraps multiple queue monitor processes into a single container
    /// </summary>
    public class QueueMonitor: IQueueMonitor
    {
        private readonly List<IMonitor> _monitors;
        private readonly IClearExpiredMessagesMonitor _clearMessagesFactory;
        private readonly IHeartBeatMonitor _heartBeatFactory;
        private readonly IHeartBeatConfiguration _heartBeatConfiguration;
        private readonly IMessageExpirationConfiguration _expirationConfiguration;
        private int _disposeCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueMonitor" /> class.
        /// </summary>
        /// <param name="clearMessagesFactory">The clear messages factory.</param>
        /// <param name="heartBeatFactory">The heart beat factory.</param>
        /// <param name="heartBeatConfiguration">The heart beat configuration.</param>
        /// <param name="expirationConfiguration">The expiration configuration.</param>
        public QueueMonitor(IClearExpiredMessagesMonitor clearMessagesFactory,
            IHeartBeatMonitor heartBeatFactory,
            IHeartBeatConfiguration heartBeatConfiguration,
            IMessageExpirationConfiguration expirationConfiguration)
        {
            Guard.NotNull(() => clearMessagesFactory, clearMessagesFactory);
            Guard.NotNull(() => heartBeatFactory, heartBeatFactory);
            Guard.NotNull(() => heartBeatConfiguration, heartBeatConfiguration);
            Guard.NotNull(() => expirationConfiguration, expirationConfiguration);

            _heartBeatConfiguration = heartBeatConfiguration;
            _heartBeatFactory = heartBeatFactory;
            _clearMessagesFactory = clearMessagesFactory;
            _expirationConfiguration = expirationConfiguration;
            _monitors = new List<IMonitor>(2);
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

            if (_heartBeatConfiguration.Enabled)
            {
                _monitors.Add(_heartBeatFactory);
            }
            if (_expirationConfiguration.Enabled)
            {
                _monitors.Add(_clearMessagesFactory);
            }
            if (_monitors.Count > 0)
            {
                _monitors.AsParallel().ForAll(w => w.Start());
            }
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
