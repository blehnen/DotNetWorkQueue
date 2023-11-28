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
using DotNetWorkQueue.Validation;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace DotNetWorkQueue.Messages
{
    internal class MessageContext : IMessageContext
    {
        private readonly Dictionary<string, object> _items = new Dictionary<string, object>();
        private int _disposeCount;

        /// <summary>
        /// Will be raised when it is time to commit the message.
        /// </summary>
        public event EventHandler Commit = delegate { };

        /// <summary>
        /// Will be raised if the message should be rolled back.
        /// </summary>
        public event EventHandler Rollback = delegate { };
        /// <summary>
        /// Will be raised after work is complete
        /// </summary>
        public event EventHandler Cleanup = delegate { };

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageContext"/> class.
        /// </summary>
        /// <param name="workerNotificationFactory">The worker notification factory.</param>
        public MessageContext(IWorkerNotificationFactory workerNotificationFactory)
        {
            Guard.NotNull(() => workerNotificationFactory, workerNotificationFactory);
            WorkerNotification = workerNotificationFactory.Create();
        }

        /// <inheritdoc/>
        public void SetMessageAndHeaders(IMessageId id, ICorrelationId correlationId, IReadOnlyDictionary<string, object> headers)
        {
            MessageId = id;
            CorrelationId = correlationId;
            Headers = headers;
        }

        /// <inheritdoc/>
        public T Get<T>(IMessageContextData<T> itemData)
            where T : class
        {
            //code may obtain user items if we are in the middle of disposing, but have not cleared the items yet
            if (IsDisposed && _items.Count == 0)
                ThrowIfDisposed();

            if (!_items.ContainsKey(itemData.Name))
            {
                _items[itemData.Name] = itemData.Default;
            }
            return (T)_items[itemData.Name];
        }

        /// <inheritdoc/>
        public void Set<T>(IMessageContextData<T> property, T value)
            where T : class
        {
            ThrowIfDisposed();
            _items[property.Name] = value;
        }

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, object> Headers { get; private set; }

        /// <inheritdoc/>
        public THeader GetHeader<THeader>(IMessageContextData<THeader> property)
            where THeader : class
        {
            if (!Headers.ContainsKey(property.Name))
            {
                return property.Default;
            }
            return (THeader)Headers[property.Name];
        }

        /// <inheritdoc/>
        public void RaiseCommit()
        {
            ThrowIfDisposed();
            Commit(this, EventArgs.Empty);
        }

        /// <inheritdoc/>
        public void RaiseRollback()
        {
            ThrowIfDisposed();
            Rollback(this, EventArgs.Empty);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Interlocked.Increment(ref _disposeCount) != 1) return;

            Cleanup(this, EventArgs.Empty);

            foreach (var obj in _items.Values)
            {
                var disposable = obj as IDisposable;
                disposable?.Dispose();
            }
            _items.Clear();
        }

        /// <inheritdoc/>
        public bool IsDisposed => Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0;

        /// <inheritdoc/>
        public IMessageId MessageId { get; private set; }

        /// <inheritdoc/>
        public ICorrelationId CorrelationId { get; private set; }

        /// <inheritdoc/>
        public IWorkerNotification WorkerNotification { get; }

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
    }
}
