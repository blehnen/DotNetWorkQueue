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

        /// <summary>
        /// Returns data set by <see cref="Set{T}" />
        /// </summary>
        /// <typeparam name="T">data type</typeparam>
        /// <param name="itemData">The item data.</param>
        /// <returns></returns>
        /// <remarks>
        /// If the data does not exist, it will be added with the default value and returned
        /// </remarks>
        public T Get<T>(IMessageContextData<T> itemData)
            where T : class
        {
            //code may obtain user items if we are in the middle of disposing, but have not cleared the items yet
            if(IsDisposed && _items.Count == 0)
                ThrowIfDisposed();
        
            if (!_items.ContainsKey(itemData.Name))
            {
                _items[itemData.Name] = itemData.Default;
            }
            return (T)_items[itemData.Name];
        }

        /// <summary>
        /// Allows the transport to attach data to the context.
        /// <remarks>For instance, data can be attached during de-queue, and then re-accessed during commit</remarks>
        /// </summary>
        /// <typeparam name="T">data type</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        public void Set<T>(IMessageContextData<T> property, T value)
            where T : class
        {
            ThrowIfDisposed();
             _items[property.Name] = value;
        }

        /// <summary>
        /// Explicitly fires the commit event.
        /// </summary>
        public void RaiseCommit()
        {
            ThrowIfDisposed();
            Commit(this, EventArgs.Empty);
        }

        /// <summary>
        /// Explicitly fires the rollback event.
        /// </summary>
        public void RaiseRollback()
        {
            ThrowIfDisposed();
            Rollback(this, EventArgs.Empty);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
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

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed => Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0;

        /// <summary>
        /// Gets or sets the message identifier.
        /// </summary>
        /// <value>
        /// The message identifier.
        /// </value>
        public IMessageId MessageId { get; set; }

        /// <summary>
        /// Worker notification data, such as stop, cancel or heartbeat failures
        /// </summary>
        /// <value>
        /// The worker notification.
        /// </value>
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
