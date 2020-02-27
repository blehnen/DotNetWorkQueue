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
using System.Collections.Concurrent;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.JobScheduler
{
    /// <summary>
    /// Creates and caches transport queues for job targets
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IJobQueue" />
    public class JobQueue : IJobQueue
    {
        private readonly ConcurrentDictionary<IConnectionInformation, IProducerMethodJobQueue> _queues;
        private readonly ConcurrentDictionary<string, IQueueContainer> _containers;
        private readonly ConcurrentDictionary<IConnectionInformation, ICreationScope> _creationScopes;
        private readonly JobQueueContainerRegistrations _registrations;

        /// <summary>
        /// Initializes a new instance of the <see cref="JobQueue"/> class.
        /// </summary>
        /// <param name="registrations">The registrations.</param>
        public JobQueue(JobQueueContainerRegistrations registrations)
        {
            Guard.NotNull(() => registrations, registrations);

            _containers = new ConcurrentDictionary<string, IQueueContainer>();
            _queues = new ConcurrentDictionary<IConnectionInformation, IProducerMethodJobQueue>();
            _creationScopes = new ConcurrentDictionary<IConnectionInformation, ICreationScope>();
            _registrations = registrations;
        }
        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed => _disposedValue;

        /// <summary>
        /// Gets the specified queue.
        /// </summary>
        /// <typeparam name="TTransportInit">The type of the transport initialize.</typeparam>
        /// <typeparam name="TQueue">The type of the queue.</typeparam>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="producerConfiguration">The producer configuration.</param>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException">Failed to create the queue. The error message is {createResult.ErrorMessage}</exception>
        public IProducerMethodJobQueue Get<TTransportInit, TQueue>(string queueName, string connection, Action<QueueProducerConfiguration> producerConfiguration = null) 
            where TTransportInit : ITransportInit, new()
            where TQueue : class, IJobQueueCreation
        {
            var connectionInfo = new BaseConnectionInformation(queueName, connection);
            if (_queues.ContainsKey(connectionInfo))
            {
                return _queues[connectionInfo];
            }

            var transportName = typeof(TTransportInit).ToString();
            if (!_containers.ContainsKey(transportName))
            {
                var container = new QueueContainer<TTransportInit>(_registrations.QueueRegistrations, _registrations.QueueOptions);
                if (!_containers.TryAdd(transportName, container))
                {
                    container.Dispose();
                }
            }

            if (!_queues.ContainsKey(connectionInfo))
            {
                using (var jobQueueCreation =
                    new JobQueueCreationContainer<TTransportInit>(_registrations.QueueCreationRegistrations, _registrations.QueueCreationOptions))
                {
                    using (var createQueue = jobQueueCreation.GetQueueCreation<TQueue>(queueName, connection))
                    {
                        var createResult = createQueue.CreateJobSchedulerQueue(_registrations.QueueCreationRegistrations, queueName, connection, _registrations.QueueCreationOptions);
                        if (createResult.Success)
                        {
                            var scope = createQueue.Scope;
                            var queue = _containers[transportName].CreateMethodJobProducer(queueName, connection);
                            producerConfiguration?.Invoke(queue.Configuration);
                            if (!_queues.TryAdd(connectionInfo, queue))
                            {
                                queue.Dispose();
                                scope.Dispose();
                            }
                            else
                            {
                                queue.Start();
                                _creationScopes.TryAdd(connectionInfo, scope);
                            }
                        }
                        else
                        {
                            throw new DotNetWorkQueueException($"Failed to create the queue. The error message is {createResult.ErrorMessage}");
                        }
                    }
                }
            }

            return _queues[connectionInfo];
        }

        /// <summary>
        /// Gets the specified queue.
        /// </summary>
        /// <typeparam name="TTransportInit">The type of the transport initialize.</typeparam>
        /// <param name="jobQueueCreation">The job queue creation.</param>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="producerConfiguration">The producer configuration.</param>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException">Failed to create the queue. The error message is {createResult.ErrorMessage}</exception>
        public IProducerMethodJobQueue Get<TTransportInit>(IJobQueueCreation jobQueueCreation, string queueName, string connection, Action<QueueProducerConfiguration> producerConfiguration = null)
           where TTransportInit : ITransportInit, new()
        {
            var connectionInfo = new BaseConnectionInformation(queueName, connection);
            if (_queues.ContainsKey(connectionInfo))
            {
                return _queues[connectionInfo];
            }

            var transportName = typeof(TTransportInit).ToString();
            if (!_containers.ContainsKey(transportName))
            {
                var container = new QueueContainer<TTransportInit>(_registrations.QueueRegistrations, _registrations.QueueOptions);
                if (!_containers.TryAdd(transportName, container))
                {
                    container.Dispose();
                }
            }

            if (!_queues.ContainsKey(connectionInfo))
            {
                var createResult = jobQueueCreation.CreateJobSchedulerQueue(_registrations.QueueCreationRegistrations,
                    queueName, connection, _registrations.QueueCreationOptions);
                if (createResult.Success)
                {
                    var scope = jobQueueCreation.Scope;
                    var queue = _containers[transportName].CreateMethodJobProducer(queueName, connection);
                    producerConfiguration?.Invoke(queue.Configuration);
                    if (!_queues.TryAdd(connectionInfo, queue))
                    {
                        queue.Dispose();
                        scope.Dispose();
                    }
                    else
                    {
                        queue.Start();
                        _creationScopes.TryAdd(connectionInfo, scope);
                    }
                }
                else
                {
                    throw new DotNetWorkQueueException($"Failed to create the queue. The error message is {createResult.ErrorMessage}");
                }
            }

            return _queues[connectionInfo];
        }

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (var queue in _queues.Values)
                    {
                        queue.Dispose();
                    }
                    foreach (var scope in _creationScopes.Values)
                    {
                        scope.Dispose();
                    }
                    foreach (var container in _containers.Values)
                    {
                        container.Dispose();
                    }
                    _queues.Clear();
                    _creationScopes.Clear();
                    _containers.Clear();
                }
                _disposedValue = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
