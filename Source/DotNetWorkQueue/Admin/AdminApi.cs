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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Admin
{
    /// <summary>
    /// Implementation of IAdminApi
    /// </summary>
    internal class AdminApi: IAdminApi
    {
        private readonly ConcurrentDictionary<Guid, Tuple<IQueueContainer, QueueConnection>> _queueConnections;
        private readonly ConcurrentDictionary<Guid, IAdminFunctions> _queueFunctions;
        private int _disposeCount;

        public AdminApi(AdminApiConfiguration configuration)
        {
            Guard.NotNull(() => configuration, configuration);
            Configuration = configuration;
            _queueConnections = new ConcurrentDictionary<Guid, Tuple<IQueueContainer, QueueConnection>>();
            _queueFunctions = new ConcurrentDictionary<Guid, IAdminFunctions>();
        }

        public IReadOnlyDictionary<Guid, Tuple<IQueueContainer, QueueConnection>> Connections => _queueConnections;

        public AdminApiConfiguration Configuration { get; }

        public Guid AddQueueConnection(IQueueContainer container, QueueConnection connection)
        {
            ThrowIfDisposed();
            Guard.NotNull(() => connection, connection);
            var id = Guid.NewGuid();
            var added = _queueConnections.TryAdd(id,
                new Tuple<DotNetWorkQueue.IQueueContainer, QueueConnection>(container, connection));

            //failing makes no sense here, but check
            if (!added)
            {
                throw new InvalidOperationException("Failed to add connection");
            }
            return id;
        }

        #region Implementations
        public long? Count(Guid id, QueueStatusAdmin? status)
        {
            ThrowIfDisposed();
            var functions = ObtainFunctions(id);
            return functions.Count(status);
        }
        #endregion

        #region Create function per connection/queue
        private IAdminFunctions ObtainFunctions(Guid id)
        {
            if (_queueFunctions.ContainsKey(id))
            {
                return _queueFunctions[id];
            }

            if (_queueConnections.TryGetValue(id, out var data))
            {
                var connection = data.Item2;
                var container = data.Item1;
                var functions = container.CreateAdminFunctions(connection);
                _queueFunctions.TryAdd(id, functions);
                return functions;
            }
            throw new InvalidOperationException($"id {id} was not found");
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
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (Interlocked.Increment(ref _disposeCount) != 1) return;

            _queueFunctions.Clear();
            _queueConnections.Clear();
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
