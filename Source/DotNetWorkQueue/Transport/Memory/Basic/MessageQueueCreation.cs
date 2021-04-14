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

namespace DotNetWorkQueue.Transport.Memory.Basic
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IQueueCreation" />
    public class MessageQueueCreation : IQueueCreation
    {
        private readonly Lazy<TransportOptions> _options;
        private int _disposeCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageQueueCreation" /> class.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="options">The options.</param>
        /// <param name="creationScope">The creation scope.</param>
        public MessageQueueCreation(IConnectionInformation connectionInfo,
            ITransportOptionsFactory options,
            ICreationScope creationScope)
        {
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => creationScope, creationScope);
            Guard.NotNull(() => connectionInfo, connectionInfo);

            _options = new Lazy<TransportOptions>(options.Create);
            ConnectionInfo = connectionInfo;
            Scope = creationScope;
        }

        /// <summary>
        /// Gets or sets the options for the queue transport.
        /// </summary>
        /// <value>
        /// The options.
        /// </value>
        public TransportOptions Options => _options.Value;

        /// <summary>
        /// Gets the base transport options.
        /// </summary>
        public IBaseTransportOptions BaseTransportOptions => _options.Value;

        /// <summary>
        /// Gets the connection information for the queue.
        /// </summary>
        /// <value>
        /// The connection information.
        /// </value>
        public IConnectionInformation ConnectionInfo { get; }

        /// <summary>
        /// Returns true if the queue exists in the transport
        /// </summary>
        /// <value>
        ///   <c>true</c> if [queue exists]; otherwise, <c>false</c>.
        /// </value>
        public bool QueueExists => true;

        /// <summary>
        /// Gets a disposable creation scope
        /// </summary>
        /// <value>
        /// The scope.
        /// </value>
        /// <remarks>This is used to prevent queues from going out of scope before you have finished working with them. Generally
        /// speaking this only matters for queues that live in-memory. However, a valid object is always returned.</remarks>
        public ICreationScope Scope { get; }

        /// <summary>
        /// Creates the queue if needed.
        /// </summary>
        /// <returns></returns>
        public QueueCreationResult CreateQueue()
        {
            return new QueueCreationResult(QueueCreationStatus.Success);
        }

        /// <summary>
        /// Attempts to delete an existing queue
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// May not be supported by all transports. Any data in the queue will be lost.
        /// </remarks>
        public QueueRemoveResult RemoveQueue()
        {
            return new QueueRemoveResult(QueueRemoveStatus.Success);
        }

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
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        ///   <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (Interlocked.Increment(ref _disposeCount) == 1)
            {

            }
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
