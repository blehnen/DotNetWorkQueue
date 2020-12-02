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
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.TaskScheduling;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue
{
    #region Creating Producer or consumer queues
    /// <summary>
    /// The root container for consumers and producers
    /// </summary>
    /// <typeparam name="TTransportInit">The type of the transport.</typeparam>
    [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
    public class QueueContainer<TTransportInit>: BaseContainer, IQueueContainer where TTransportInit : ITransportInit, new()
    {
        // ReSharper disable once StaticMemberInGenericType
        private static Func<ICreateContainer<TTransportInit>> _createContainerInternal = () => new CreateContainer<TTransportInit>();
        private readonly Action<IContainer> _registerService;
        private readonly Action<IContainer> _setOptions;
        private readonly TTransportInit _transportInit;

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
        /// <param name="setOptions">The options.</param>
        public QueueContainer(Action<IContainer> registerService, Action<IContainer> setOptions = null)
        {
            _registerService = registerService;
            _setOptions = setOptions;
            _transportInit = new TTransportInit();
        }
        #endregion

        #region Consuming messages queue

        /// <summary>
        /// Creates the consumer queue.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        public IConsumerQueue CreateConsumer(QueueConnection queueConnection)
        {
            Guard.NotNull(() => queueConnection, queueConnection);

            var container = _createContainerInternal().Create(QueueContexts.ConsumerQueue, _registerService, queueConnection, _transportInit, ConnectionTypes.Receive, x => { }, _setOptions);
            Containers.Add(container);
            return container.GetInstance<IConsumerQueue>();
        }

        /// <summary>
        /// Creates the method consumer queue.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        public IConsumerMethodQueue CreateMethodConsumer(QueueConnection queueConnection)
        {
            Guard.NotNull(() => queueConnection, queueConnection);

            var container = _createContainerInternal().Create(QueueContexts.ConsumerMethodQueue, _registerService, queueConnection, _transportInit, ConnectionTypes.Receive, x => { }, _setOptions);
            Containers.Add(container);
            return container.GetInstance<IConsumerMethodQueue>();
        }

        /// <summary>
        /// Creates an async consumer queue
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        public IConsumerQueueAsync CreateConsumerAsync(QueueConnection queueConnection)
        {
            Guard.NotNull(() => queueConnection, queueConnection);

            var container = _createContainerInternal().Create(QueueContexts.ConsumerQueueAsync, _registerService, queueConnection, _transportInit, ConnectionTypes.Receive, x => { }, _setOptions);
            Containers.Add(container);
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
        public IConsumerQueueScheduler CreateConsumerQueueScheduler(QueueConnection queueConnection)
        {
            Guard.NotNull(() => queueConnection, queueConnection);

            var schedulerCreator = new SchedulerContainer(_registerService);
            var factory = schedulerCreator.CreateTaskFactory();
            factory.Scheduler.Start();
            Containers.Add(schedulerCreator);
            return CreateConsumerQueueSchedulerInternal(queueConnection, factory, null, true);
        }

        /// <summary>
        /// Creates an async consumer queue that uses a task scheduler. The default task factory will be used.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        public IConsumerMethodQueueScheduler CreateConsumerMethodQueueScheduler(QueueConnection queueConnection)
        {
            Guard.NotNull(() => queueConnection, queueConnection);
   
            var schedulerCreator = new SchedulerContainer(_registerService);
            var factory = schedulerCreator.CreateTaskFactory();
            factory.Scheduler.Start();
            Containers.Add(schedulerCreator);
            return CreateConsumerMethodQueueSchedulerInternal(queueConnection, factory, null, true);
        }

        /// <summary>
        /// Creates an async consumer queue that uses a task scheduler
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="factory">The task factory.</param>
        /// <returns></returns>
        public IConsumerQueueScheduler CreateConsumerQueueScheduler(QueueConnection queueConnection, ITaskFactory factory)
        {
            Guard.NotNull(() => queueConnection, queueConnection);
 
            return CreateConsumerQueueSchedulerInternal(queueConnection, factory, null, false);
        }
        /// <summary>
        /// Creates an async consumer queue that uses a task scheduler
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="factory">The task factory.</param>
        /// <returns></returns>
        public IConsumerMethodQueueScheduler CreateConsumerMethodQueueScheduler(QueueConnection queueConnection, ITaskFactory factory)
        {
            Guard.NotNull(() => queueConnection, queueConnection);

            return CreateConsumerMethodQueueSchedulerInternal(queueConnection, factory, null, false);
        }
        /// <summary>
        /// Creates an async consumer queue that uses a task scheduler
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="factory">The task factory.</param>
        /// <param name="workGroup">The work group.</param>
        /// <returns></returns>
        public IConsumerMethodQueueScheduler CreateConsumerMethodQueueScheduler(QueueConnection queueConnection, ITaskFactory factory, IWorkGroup workGroup)
        {
            Guard.NotNull(() => queueConnection, queueConnection);
    
            return CreateConsumerMethodQueueSchedulerInternal(queueConnection, factory, workGroup, false);
        }
        /// <summary>
        /// Creates an async consumer queue that uses a task scheduler
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="factory">The task factory.</param>
        /// <param name="workGroup">The work group.</param>
        /// <returns></returns>
        public IConsumerQueueScheduler CreateConsumerQueueScheduler(QueueConnection queueConnection, ITaskFactory factory, IWorkGroup workGroup)
        {
            Guard.NotNull(() => queueConnection, queueConnection);
     
            return CreateConsumerQueueSchedulerInternal(queueConnection, factory, workGroup, false);
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
        private IConsumerQueueScheduler CreateConsumerQueueSchedulerInternal(QueueConnection queueConnection, 
            ITaskFactory factory, IWorkGroup workGroup, bool internalFactory)
        {
            Guard.NotNull(() => queueConnection, queueConnection);
            IContainer container;
            if (internalFactory) //we own the factory
            {
                if (workGroup == null)
                {
                    container = _createContainerInternal().Create(QueueContexts.ConsumerQueueScheduler, _registerService, queueConnection, _transportInit, ConnectionTypes.Receive,
                        serviceRegister => serviceRegister.Register(() => factory, LifeStyles.Singleton).Register<IWorkGroup>(() => new WorkGroupNoOp(), LifeStyles.Singleton).
                        Register(() => factory.Scheduler, LifeStyles.Singleton), _setOptions);
                }
                else
                {
                    container = _createContainerInternal().Create(QueueContexts.ConsumerQueueScheduler, _registerService, queueConnection, _transportInit, ConnectionTypes.Receive,
                         serviceRegister => serviceRegister.Register(() => factory, LifeStyles.Singleton).Register(() => workGroup, LifeStyles.Singleton).
                        Register(() => factory.Scheduler, LifeStyles.Singleton), _setOptions);
                }
            }
            else //someone else owns the factory
            {
                if (workGroup == null)
                {
                    container = _createContainerInternal().Create(QueueContexts.ConsumerQueueScheduler, _registerService, queueConnection, _transportInit, ConnectionTypes.Receive,
                        serviceRegister => serviceRegister.RegisterNonScopedSingleton(factory).Register<IWorkGroup>(() => new WorkGroupNoOp(), LifeStyles.Singleton).
                        RegisterNonScopedSingleton(factory.Scheduler), _setOptions);
                }
                else
                {
                    container = _createContainerInternal().Create(QueueContexts.ConsumerQueueScheduler, _registerService, queueConnection, _transportInit, ConnectionTypes.Receive,
                         serviceRegister => serviceRegister.RegisterNonScopedSingleton(factory).Register(() => workGroup, LifeStyles.Singleton).
                        RegisterNonScopedSingleton(factory.Scheduler), _setOptions);
                }
            }
            Containers.Add(container);
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
        private IConsumerMethodQueueScheduler CreateConsumerMethodQueueSchedulerInternal(QueueConnection queueConnection, ITaskFactory factory, IWorkGroup workGroup, bool internalFactory)
        {
            Guard.NotNull(() => queueConnection, queueConnection);
 
            IContainer container;
            if (internalFactory) //we own factory
            {
                if (workGroup == null)
                {
                    container = _createContainerInternal()
                        .Create(QueueContexts.ConsumerMethodQueueScheduler, _registerService, queueConnection,
                            _transportInit, ConnectionTypes.Receive,
                            serviceRegister =>
                                serviceRegister.Register(() => factory, LifeStyles.Singleton)
                                    .Register<IWorkGroup>(() => new WorkGroupNoOp(), LifeStyles.Singleton)
                                    .Register(() => factory.Scheduler, LifeStyles.Singleton), _setOptions);
                }
                else
                {
                    container = _createContainerInternal()
                        .Create(QueueContexts.ConsumerMethodQueueScheduler, _registerService, queueConnection,
                            _transportInit, ConnectionTypes.Receive,
                            serviceRegister =>
                                serviceRegister.Register(() => factory, LifeStyles.Singleton)
                                    .Register(() => workGroup, LifeStyles.Singleton)
                                    .Register(() => factory.Scheduler, LifeStyles.Singleton), _setOptions);
                }
            }
            else  //someone else owns factory
            {
                if (workGroup == null)
                {
                    container = _createContainerInternal()
                        .Create(QueueContexts.ConsumerMethodQueueScheduler, _registerService, queueConnection,
                            _transportInit, ConnectionTypes.Receive,
                            serviceRegister =>
                                serviceRegister.RegisterNonScopedSingleton(factory)
                                    .Register<IWorkGroup>(() => new WorkGroupNoOp(), LifeStyles.Singleton)
                                    .RegisterNonScopedSingleton(factory.Scheduler), _setOptions);
                }
                else
                {
                    container = _createContainerInternal()
                        .Create(QueueContexts.ConsumerMethodQueueScheduler, _registerService, queueConnection,
                            _transportInit, ConnectionTypes.Receive,
                            serviceRegister =>
                                serviceRegister.RegisterNonScopedSingleton(factory)
                                    .Register(() => workGroup, LifeStyles.Singleton)
                                    .RegisterNonScopedSingleton(factory.Scheduler), _setOptions);
                }
            }
            Containers.Add(container);
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
            QueueConnection queueConnection)
            where TMessage : class
        {
            Guard.NotNull(() => queueConnection, queueConnection);
   
            var container = _createContainerInternal().Create(QueueContexts.ProducerQueue, _registerService, queueConnection, _transportInit, ConnectionTypes.Send, x => { }, _setOptions);
            Containers.Add(container);
            return container.GetInstance<IProducerQueue<TMessage>>();
        }

        /// <summary>
        /// Creates a producer queue for executing linq expressions.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        public IProducerMethodQueue CreateMethodProducer(
            QueueConnection queueConnection)
        {
            Guard.NotNull(() => queueConnection, queueConnection);
  
            var container = _createContainerInternal().Create(QueueContexts.ProducerMethodQueue, _registerService, queueConnection, _transportInit, ConnectionTypes.Send, x => { }, _setOptions);
            Containers.Add(container);
            return container.GetInstance<IProducerMethodQueue>();
        }

        /// <summary>
        /// Creates a producer queue for executing linq expressions.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        public IProducerMethodJobQueue CreateMethodJobProducer(
            QueueConnection queueConnection)
        {
            Guard.NotNull(() => queueConnection, queueConnection);
 
            var container = _createContainerInternal().Create(QueueContexts.ProducerMethodQueue, _registerService, queueConnection, _transportInit, ConnectionTypes.Send, x => { }, _setOptions);
            Containers.Add(container);
            return container.GetInstance<IProducerMethodJobQueue>();
        }

        #endregion

        #region Job Scheduler
        /// <summary>
        /// Creates a re-occurring job scheduler.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        public IJobSchedulerLastKnownEvent CreateJobSchedulerLastKnownEvent(QueueConnection queueConnection)
        {
            var container = _createContainerInternal().Create(QueueContexts.ProducerMethodQueue, _registerService, queueConnection, _transportInit, ConnectionTypes.Send, x => { }, _setOptions);
            Containers.Add(container);
            return container.GetInstance<IJobSchedulerLastKnownEvent>();
        }
        #endregion

        #region Time Sync
        /// <summary>
        /// Creates a class that returns the current date/time
        /// </summary>
        /// <returns></returns>
        public IGetTimeFactory CreateTimeSync(string connection)
        {
            var container = _createContainerInternal().Create(QueueContexts.Time, _registerService, new QueueConnection("TIME", connection), _transportInit, ConnectionTypes.Send, x => { }, _setOptions);
            var factory = container.GetInstance<IGetTimeFactory>();
            return factory;
        }
        #endregion
    }
    #endregion 
}
