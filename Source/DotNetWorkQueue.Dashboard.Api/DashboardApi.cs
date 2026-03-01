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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Dashboard.Api.Configuration;

namespace DotNetWorkQueue.Dashboard.Api
{
    /// <summary>
    /// Implementation of <see cref="IDashboardApi"/> that manages queue containers and admin functions.
    /// </summary>
    internal class DashboardApi : IDashboardApi
    {
        private readonly Dictionary<Guid, DashboardConnectionInfo> _connections;
        private readonly Dictionary<Guid, DashboardQueueInfo> _queues;
        private readonly List<IQueueContainer> _queueContainers;
        private readonly ConcurrentDictionary<Guid, IContainer> _adminContainers;
        private readonly ConcurrentDictionary<Guid, Tuple<IQueueContainer, QueueConnection, Action<IContainer>>> _queueContainerMap;
        private int _disposeCount;

        /// <summary>
        /// Creates a new DashboardApi from the provided options.
        /// </summary>
        /// <param name="options">The dashboard configuration options.</param>
        public DashboardApi(DashboardOptions options)
        {
            _connections = new Dictionary<Guid, DashboardConnectionInfo>();
            _queues = new Dictionary<Guid, DashboardQueueInfo>();
            _queueContainers = new List<IQueueContainer>();
            _adminContainers = new ConcurrentDictionary<Guid, IContainer>();
            _queueContainerMap = new ConcurrentDictionary<Guid, Tuple<IQueueContainer, QueueConnection, Action<IContainer>>>();

            InitializeFromOptions(options);
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<Guid, DashboardConnectionInfo> Connections => _connections;

        /// <inheritdoc />
        public DashboardQueueInfo FindQueue(Guid queueId)
        {
            ThrowIfDisposed();
            _queues.TryGetValue(queueId, out var info);
            return info;
        }

        /// <inheritdoc />
        public IContainer GetQueueContainer(Guid queueId)
        {
            ThrowIfDisposed();

            if (_adminContainers.TryGetValue(queueId, out var existing))
                return existing;

            if (!_queueContainerMap.TryGetValue(queueId, out var data))
                throw new InvalidOperationException($"Queue id {queueId} was not found");

            var container = data.Item1.CreateAdminContainer(data.Item2, data.Item3);
            _adminContainers.TryAdd(queueId, container);
            return container;
        }

        private void InitializeFromOptions(DashboardOptions options)
        {
            foreach (var registration in options.ConnectionRegistrations)
            {
                var connectionId = Guid.NewGuid();
                var queueContainer = CreateQueueContainer(
                    registration.TransportInitType,
                    registration.ContainerConfig);
                _queueContainers.Add(queueContainer);

                var queueInfos = new List<DashboardQueueInfo>();
                foreach (var queueOption in registration.Queues)
                {
                    var queueId = Guid.NewGuid();
                    var queueConnection = new QueueConnection(
                        queueOption.QueueName,
                        registration.ConnectionString);

                    var queueInfo = new DashboardQueueInfo
                    {
                        Id = queueId,
                        ConnectionId = connectionId,
                        QueueName = queueOption.QueueName,
                        ConnectionString = registration.ConnectionString,
                        InterceptorConfiguration = queueOption.InterceptorConfiguration
                    };

                    _queues[queueId] = queueInfo;
                    _queueContainerMap[queueId] = new Tuple<IQueueContainer, QueueConnection, Action<IContainer>>(queueContainer, queueConnection, queueOption.InterceptorConfiguration);
                    queueInfos.Add(queueInfo);
                }

                _connections[connectionId] = new DashboardConnectionInfo
                {
                    Id = connectionId,
                    ConnectionString = registration.ConnectionString,
                    DisplayName = registration.DisplayName,
                    Queues = queueInfos
                };
            }
        }

        private static IQueueContainer CreateQueueContainer(Type transportInitType, Action<IContainer> containerConfig)
        {
            var containerType = typeof(QueueContainer<>).MakeGenericType(transportInitType);

            if (containerConfig != null)
            {
                return (IQueueContainer)Activator.CreateInstance(containerType, new object[] { containerConfig, null });
            }

            return (IQueueContainer)Activator.CreateInstance(containerType);
        }

        #region IDisposable

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
        /// Releases managed resources.
        /// </summary>
        /// <param name="disposing">true to release managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (Interlocked.Increment(ref _disposeCount) != 1) return;

            _adminContainers.Clear();
            _queueContainerMap.Clear();

            foreach (var container in _queueContainers)
            {
                try
                {
                    container.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // The host application may have already disposed this container
                    // before the dashboard shuts down; safe to ignore during teardown.
                }
            }

            _queueContainers.Clear();
        }

        #endregion
    }
}
