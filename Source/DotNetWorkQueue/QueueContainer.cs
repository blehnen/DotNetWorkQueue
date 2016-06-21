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
using System.Collections.Concurrent;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Factory;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.QueueStatus;
using DotNetWorkQueue.TaskScheduling;
namespace DotNetWorkQueue
{
    #region Creating Producer or consumer queues
    /// <summary>
    /// The root container for consumers and producers
    /// </summary>
    /// <typeparam name="TTransportInit">The type of the transport.</typeparam>
    public class QueueContainer<TTransportInit>: IQueueContainer where TTransportInit : ITransportInit, new()
    {
        // ReSharper disable once StaticMemberInGenericType
        private static Func<ICreateContainer<TTransportInit>> _createContainerInternal = () => new CreateContainer<TTransportInit>();
        private readonly Action<IContainer> _registerService;
        private readonly TTransportInit _transportInit;

        private readonly ConcurrentBag<IDisposable> _containers; 

        /// <summary>
        /// Set the container creation function. This allows you to use your own IoC container.
        /// </summary>
        public static void SetContainerFactory(Func<ICreateContainer<TTransportInit>> createContainer)
        {
            Guard.NotNull(() => createContainer, createContainer);
            _createContainerInternal = createContainer;
        }

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueContainer{TTransportInit}"/> class.
        /// </summary>
        public QueueContainer()
            : this(x => { })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueContainer{TTransportInit}" /> class.
        /// </summary>
        /// <param name="registerService">The register service.</param>
        public QueueContainer(Action<IContainer> registerService)
        {
            _containers = new ConcurrentBag<IDisposable>();
            _registerService = registerService;
            _transportInit = new TTransportInit();
        }
        #endregion


        #region Dispose
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
            lock (_containers)
            {
                while (!_containers.IsEmpty)
                {
                    IDisposable item;
                    if (_containers.TryTake(out item))
                    {
                        item?.Dispose();
                    }
                }
            }
        }
        #endregion

        /// <summary>
        /// Creates a new status provider.
        /// </summary>
        /// <returns></returns>
        public IQueueStatusProvider CreateStatusProvider(string queue, string connection)
        {
            Guard.NotNullOrEmpty(() => queue, queue);
            Guard.NotNullOrEmpty(() => connection, connection);

            try
            {
                //disable the time client and the system information module, as they may provide incorrect information when the status provider is not created by the queue itself
                var container = _createContainerInternal().Create(QueueContexts.QueueStatus, _registerService, queue,
                    connection, _transportInit, ConnectionTypes.Status, serviceRegister =>
                    serviceRegister.Register<IGetTimeFactory, GetTimeFactoryNoOp>(LifeStyles.Singleton));
                _containers.Add(container);
                return container.GetInstance<IQueueStatusProvider>();
            }
            catch (Exception error)
            {
                return new QueueStatusProviderError<TTransportInit>(queue, connection, this, error);
            }
        }

        #region Consuming messages queue

        /// <summary>
        /// Creates the consumer queue.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        public IConsumerQueue CreateConsumer(string queue, string connection)
        {
            Guard.NotNullOrEmpty(() => queue, queue);
            Guard.NotNullOrEmpty(() => connection, connection);

            var container = _createContainerInternal().Create(QueueContexts.ConsumerQueue, _registerService, queue,
                connection, _transportInit, ConnectionTypes.Receive, x => { });
            _containers.Add(container);
            return container.GetInstance<IConsumerQueue>();
        }

        /// <summary>
        /// Creates the method consumer queue.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        public IConsumerMethodQueue CreateMethodConsumer(string queue, string connection)
        {
            Guard.NotNullOrEmpty(() => queue, queue);
            Guard.NotNullOrEmpty(() => connection, connection);

            var container = _createContainerInternal().Create(QueueContexts.ConsumerMethodQueue, _registerService, queue,
                connection, _transportInit, ConnectionTypes.Receive, x => { });
            _containers.Add(container);
            return container.GetInstance<IConsumerMethodQueue>();
        }

        /// <summary>
        /// Creates an async consumer queue
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        public IConsumerQueueAsync CreateConsumerAsync(string queue,
            string connection)
        {
            Guard.NotNullOrEmpty(() => queue, queue);
            Guard.NotNullOrEmpty(() => connection, connection);

            var container = _createContainerInternal().Create(QueueContexts.ConsumerQueueAsync, _registerService, queue,
                connection, _transportInit, ConnectionTypes.Receive, x => { });
            _containers.Add(container);
            return container.GetInstance<IConsumerQueueAsync>();
        }
        #endregion

        #region Scheduler Queue
        /// <summary>
        /// Creates an async consumer queue that uses a task scheduler. The default task factory will be used.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        public IConsumerQueueScheduler CreateConsumerQueueScheduler(string queue, string connection)
        {
            Guard.NotNullOrEmpty(() => queue, queue);
            Guard.NotNullOrEmpty(() => connection, connection);

            var schedulerCreator = new SchedulerContainer(_registerService);
            var factory = schedulerCreator.CreateTaskFactory();
            factory.Scheduler.Start();
            _containers.Add(schedulerCreator);
            return CreateConsumerQueueSchedulerInternal(queue, connection, factory, null, true);
        }

        /// <summary>
        /// Creates an async consumer queue that uses a task scheduler. The default task factory will be used.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        public IConsumerMethodQueueScheduler CreateConsumerMethodQueueScheduler(string queue, string connection)
        {
            Guard.NotNullOrEmpty(() => queue, queue);
            Guard.NotNullOrEmpty(() => connection, connection);

            var schedulerCreator = new SchedulerContainer(_registerService);
            var factory = schedulerCreator.CreateTaskFactory();
            factory.Scheduler.Start();
            _containers.Add(schedulerCreator);
            return CreateConsumerMethodQueueSchedulerInternal(queue, connection, factory, null, true);
        }

        /// <summary>
        /// Creates an async consumer queue that uses a task scheduler
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="factory">The task factory.</param>
        /// <returns></returns>
        public IConsumerQueueScheduler CreateConsumerQueueScheduler(string queue, string connection, ITaskFactory factory)
        {
            Guard.NotNullOrEmpty(() => queue, queue);
            Guard.NotNullOrEmpty(() => connection, connection);

            return CreateConsumerQueueSchedulerInternal(queue, connection, factory, null, false);
        }
        /// <summary>
        /// Creates an async consumer queue that uses a task scheduler
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="factory">The task factory.</param>
        /// <returns></returns>
        public IConsumerMethodQueueScheduler CreateConsumerMethodQueueScheduler(string queue, string connection, ITaskFactory factory)
        {
            Guard.NotNullOrEmpty(() => queue, queue);
            Guard.NotNullOrEmpty(() => connection, connection);

            return CreateConsumerMethodQueueSchedulerInternal(queue, connection, factory, null, false);
        }
        /// <summary>
        /// Creates an async consumer queue that uses a task scheduler
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="factory">The task factory.</param>
        /// <param name="workGroup">The work group.</param>
        /// <returns></returns>
        public IConsumerMethodQueueScheduler CreateConsumerMethodQueueScheduler(string queue, string connection, ITaskFactory factory, IWorkGroup workGroup)
        {
            Guard.NotNullOrEmpty(() => queue, queue);
            Guard.NotNullOrEmpty(() => connection, connection);

            return CreateConsumerMethodQueueSchedulerInternal(queue, connection, factory, workGroup, false);
        }
        /// <summary>
        /// Creates an async consumer queue that uses a task scheduler
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="factory">The task factory.</param>
        /// <param name="workGroup">The work group.</param>
        /// <returns></returns>
        public IConsumerQueueScheduler CreateConsumerQueueScheduler(string queue, string connection, ITaskFactory factory, IWorkGroup workGroup)
        {
            Guard.NotNullOrEmpty(() => queue, queue);
            Guard.NotNullOrEmpty(() => connection, connection);

            return CreateConsumerQueueSchedulerInternal(queue, connection, factory, workGroup, false);
        }

        /// <summary>
        /// Creates an async consumer queue that uses a task scheduler
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="factory">The factory.</param>
        /// <param name="workGroup">The work group.</param>
        /// <param name="internalFactory">if set to <c>true</c> [internal factory].</param>
        /// <returns></returns>
        private IConsumerQueueScheduler CreateConsumerQueueSchedulerInternal(string queue,
            string connection, ITaskFactory factory, IWorkGroup workGroup, bool internalFactory)
        {
            Guard.NotNullOrEmpty(() => queue, queue);
            Guard.NotNullOrEmpty(() => connection, connection);

            IContainer container;
            if (internalFactory) //we own the factory
            {
                if (workGroup == null)
                {
                    container = _createContainerInternal().Create(QueueContexts.ConsumerQueueScheduler, _registerService, queue, connection, _transportInit, ConnectionTypes.Receive,
                        serviceRegister => serviceRegister.Register(() => factory, LifeStyles.Singleton).Register<IWorkGroup>(() => new WorkGroupNoOp(), LifeStyles.Singleton).
                        Register(() => factory.Scheduler, LifeStyles.Singleton));
                }
                else
                {
                    container = _createContainerInternal().Create(QueueContexts.ConsumerQueueScheduler, _registerService, queue, connection, _transportInit, ConnectionTypes.Receive,
                         serviceRegister => serviceRegister.Register(() => factory, LifeStyles.Singleton).Register(() => workGroup, LifeStyles.Singleton).
                        Register(() => factory.Scheduler, LifeStyles.Singleton));
                }
            }
            else //someone else owns the factory
            {
                if (workGroup == null)
                {
                    container = _createContainerInternal().Create(QueueContexts.ConsumerQueueScheduler, _registerService, queue, connection, _transportInit, ConnectionTypes.Receive,
                        serviceRegister => serviceRegister.RegisterNonScopedSingleton(factory).Register<IWorkGroup>(() => new WorkGroupNoOp(), LifeStyles.Singleton).
                        RegisterNonScopedSingleton(factory.Scheduler));
                }
                else
                {
                    container = _createContainerInternal().Create(QueueContexts.ConsumerQueueScheduler, _registerService, queue, connection, _transportInit, ConnectionTypes.Receive,
                         serviceRegister => serviceRegister.RegisterNonScopedSingleton(factory).Register(() => workGroup, LifeStyles.Singleton).
                        RegisterNonScopedSingleton(factory.Scheduler));
                }
            }
            _containers.Add(container);
            return container.GetInstance<IConsumerQueueScheduler>();
        }

        /// <summary>
        /// Creates the consumer method queue scheduler.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="factory">The factory.</param>
        /// <param name="workGroup">The work group.</param>
        /// <param name="internalFactory">if set to <c>true</c> [internal factory].</param>
        /// <returns></returns>
        private IConsumerMethodQueueScheduler CreateConsumerMethodQueueSchedulerInternal(string queue,
            string connection, ITaskFactory factory, IWorkGroup workGroup, bool internalFactory)
        {
            Guard.NotNullOrEmpty(() => queue, queue);
            Guard.NotNullOrEmpty(() => connection, connection);

            IContainer container;
            if (internalFactory) //we own factory
            {
                if (workGroup == null)
                {
                    container = _createContainerInternal()
                        .Create(QueueContexts.ConsumerMethodQueueScheduler, _registerService, queue, connection,
                            _transportInit, ConnectionTypes.Receive,
                            serviceRegister =>
                                serviceRegister.Register(() => factory, LifeStyles.Singleton)
                                    .Register<IWorkGroup>(() => new WorkGroupNoOp(), LifeStyles.Singleton)
                                    .Register(() => factory.Scheduler, LifeStyles.Singleton));
                }
                else
                {
                    container = _createContainerInternal()
                        .Create(QueueContexts.ConsumerMethodQueueScheduler, _registerService, queue, connection,
                            _transportInit, ConnectionTypes.Receive,
                            serviceRegister =>
                                serviceRegister.Register(() => factory, LifeStyles.Singleton)
                                    .Register(() => workGroup, LifeStyles.Singleton)
                                    .Register(() => factory.Scheduler, LifeStyles.Singleton));
                }
            }
            else  //someone else owns factory
            {
                if (workGroup == null)
                {
                    container = _createContainerInternal()
                        .Create(QueueContexts.ConsumerMethodQueueScheduler, _registerService, queue, connection,
                            _transportInit, ConnectionTypes.Receive,
                            serviceRegister =>
                                serviceRegister.RegisterNonScopedSingleton(factory)
                                    .Register<IWorkGroup>(() => new WorkGroupNoOp(), LifeStyles.Singleton)
                                    .RegisterNonScopedSingleton(factory.Scheduler));
                }
                else
                {
                    container = _createContainerInternal()
                        .Create(QueueContexts.ConsumerMethodQueueScheduler, _registerService, queue, connection,
                            _transportInit, ConnectionTypes.Receive,
                            serviceRegister =>
                                serviceRegister.RegisterNonScopedSingleton(factory)
                                    .Register(() => workGroup, LifeStyles.Singleton)
                                    .RegisterNonScopedSingleton(factory.Scheduler));
                }
            }
            _containers.Add(container);
            return container.GetInstance<IConsumerMethodQueueScheduler>();
        }
        #endregion

        #region Producer queue

        /// <summary>
        /// Creates a producer queue.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        public IProducerQueue<TMessage> CreateProducer<TMessage>(
            string queue, string connection)
            where TMessage : class
        {
            Guard.NotNullOrEmpty(() => queue, queue);
            Guard.NotNullOrEmpty(() => connection, connection);

            var container = _createContainerInternal().Create(QueueContexts.ProducerQueue, _registerService, queue,
                connection, _transportInit, ConnectionTypes.Send, x => { });
            _containers.Add(container);
            return container.GetInstance<IProducerQueue<TMessage>>();
        }

        /// <summary>
        /// Creates a producer queue for executing linq expressions.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        public IProducerMethodQueue CreateMethodProducer(
            string queue, string connection)
        {
            Guard.NotNullOrEmpty(() => queue, queue);
            Guard.NotNullOrEmpty(() => connection, connection);

            var container = _createContainerInternal().Create(QueueContexts.ProducerMethodQueue, _registerService, queue,
                connection, _transportInit, ConnectionTypes.Send, x => { });
            _containers.Add(container);
            return container.GetInstance<IProducerMethodQueue>();
        }

        #endregion

        #region RPC Queue

        /// <summary>
        /// Creates an RPC queue.
        /// </summary>
        /// <typeparam name="TMessageReceive">The type of the received message.</typeparam>
        /// <typeparam name="TMessageSend">The type of the sent message.</typeparam>
        /// <typeparam name="TTConnectionSettings">The type of the connection settings.</typeparam>
        /// <param name="connectonSettings">The connection settings.</param>
        /// <returns></returns>
        public IRpcQueue<TMessageReceive, TMessageSend> CreateRpc
            <TMessageReceive, TMessageSend, TTConnectionSettings>(TTConnectionSettings connectonSettings)
            where TMessageReceive : class
            where TMessageSend : class
            where TTConnectionSettings : BaseRpcConnection
        {
            //we need to create an explicit producer queue
            var connectionSend = connectonSettings.GetConnection(ConnectionTypes.Send);
            var producer = CreateProducer<TMessageSend>(connectionSend.QueueName, connectionSend.ConnectionString);

            var connectionReceive = connectonSettings.GetConnection(ConnectionTypes.Receive);
            var container = _createContainerInternal().Create(QueueContexts.RpcQueue, _registerService,
                connectionReceive.QueueName, connectionReceive.ConnectionString, _transportInit, ConnectionTypes.Receive,
                serviceRegister =>
                    serviceRegister.Register(
                        () => producer, LifeStyles.Singleton).Register(
                        () => connectonSettings, LifeStyles.Singleton));
            _containers.Add(container);
            return
                container
                    .GetInstance<IRpcQueue<TMessageReceive, TMessageSend>>();
        }

        /// <summary>
        /// Creates an RPC queue.
        /// </summary>
        /// <typeparam name="TTConnectionSettings">The type of the connection settings.</typeparam>
        /// <param name="connectonSettings">The connection settings.</param>
        /// <returns></returns>
        public IRpcMethodQueue CreateMethodRpc
            <TTConnectionSettings>(TTConnectionSettings connectonSettings)
            where TTConnectionSettings : BaseRpcConnection
        {
            //create a base RPC queue for usage by the method RPC queue
            var rpc = CreateRpc<object, MessageExpression, TTConnectionSettings>(connectonSettings);
            var connectionReceive = connectonSettings.GetConnection(ConnectionTypes.Receive);
            var container = _createContainerInternal().Create(QueueContexts.RpcMethodQueue, _registerService,
                connectionReceive.QueueName, connectionReceive.ConnectionString, _transportInit, ConnectionTypes.Receive,
                serviceRegister =>
                    serviceRegister.Register(
                        () => rpc, LifeStyles.Singleton).Register(
                        () => connectonSettings, LifeStyles.Singleton));
            _containers.Add(container);
            return
                container
                    .GetInstance<IRpcMethodQueue>();
        }

        /// <summary>
        /// Creates an RPC queue that can send responses
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        public IProducerQueueRpc<TMessage> CreateProducerRpc
            <TMessage>(string queue, string connection)
            where TMessage : class
        {
            Guard.NotNullOrEmpty(() => queue, queue);
            Guard.NotNullOrEmpty(() => connection, connection);

            var container = _createContainerInternal().Create(QueueContexts.ProducerQueueRpc, _registerService, queue,
                connection, _transportInit, ConnectionTypes.Send, x => { });
            _containers.Add(container);
            return container.GetInstance<IProducerQueueRpc<TMessage>>();
        }

        #endregion   
    }
    #endregion 
}
