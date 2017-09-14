// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using System.Linq;
using CacheManager.Core;
using DotNetWorkQueue.Cache;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Factory;
using DotNetWorkQueue.Interceptors;
using DotNetWorkQueue.JobScheduler;
using DotNetWorkQueue.LinqCompile;
using DotNetWorkQueue.LinqCompile.Decorator;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Logging.Decorator;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Metrics.Decorator;
using DotNetWorkQueue.Metrics.NoOp;
using DotNetWorkQueue.Policies;
using DotNetWorkQueue.Policies.Decorator;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.QueueStatus;
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.TaskScheduling;
using DotNetWorkQueue.Time;
using DotNetWorkQueue.Validation;
using Polly;
using Polly.Registry;
using ClearExpiredMessagesDecorator = DotNetWorkQueue.Logging.Decorator.ClearExpiredMessagesDecorator;
using ReceivePoisonMessageDecorator = DotNetWorkQueue.Logging.Decorator.ReceivePoisonMessageDecorator;
using ResetHeartBeatDecorator = DotNetWorkQueue.Logging.Decorator.ResetHeartBeatDecorator;
using RollbackMessageDecorator = DotNetWorkQueue.Metrics.Decorator.RollbackMessageDecorator;

namespace DotNetWorkQueue.IoC
{
    /// <summary>
    /// Registers internal components in the internal IoC container
    /// </summary>
    public class ComponentRegistration
    {
        /// <summary>
        /// Registers the defaults implementations.
        /// </summary>
        /// <param name="container">The container.</param>
        public static void RegisterDefaultsForScheduler(IContainer container)
        {
            Guard.NotNull(() => container, container);

            RegisterSharedDefaults(container);

            container.Register<ITaskSchedulerConfiguration, TaskSchedulerConfiguration>(LifeStyles.Singleton);
            container.Register<ATaskScheduler, SmartThreadPoolTaskScheduler>(LifeStyles.Singleton);
            container.Register<ITaskFactory, SchedulerTaskFactory>(LifeStyles.Singleton);
            container.Register<IWaitForEventOrCancelThreadPool, WaitForEventOrCancelThreadPool>(LifeStyles.Singleton);
            container.Register<IWaitForEventOrCancelFactory, WaitForEventOrCancelFactory>(LifeStyles.Singleton);
            container.Register<SchedulerMessageHandler, SchedulerMessageHandler>(LifeStyles.Singleton);
            container.Register<ITaskSchedulerFactory, TaskSchedulerFactory>(LifeStyles.Singleton);
            container.Register<ITaskFactoryFactory, TaskFactoryFactory>(LifeStyles.Singleton);
            container.Register<IMetrics, MetricsNoOp>(LifeStyles.Singleton);         
        }

        /// <summary>
        /// Registers the defaults implementations.
        /// </summary>
        /// <param name="container">The container.</param>
        public static void RegisterDefaultsForJobScheduler(IContainer container)
        {
            Guard.NotNull(() => container, container);
            RegisterSharedDefaults(container);
            container.Register<IMetrics, MetricsNoOp>(LifeStyles.Singleton);
        }

        /// <summary>
        /// Registers the defaults implementations.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="registrationType">Type of the registration.</param>
        public static void RegisterDefaults(IContainer container,
            RegistrationTypes registrationType)
        {
            Guard.NotNull(() => container, container);

            //default types that are always registered in the container for both send and receive
            RegisterSharedDefaults(container);

            //object cache
            container.Register(() => CacheFactory.Build("DotNetWorkQueueCache", settings =>
            {
#if NETFULL
                settings.WithSystemRuntimeCacheHandle("LinqCache");
#else
                settings.WithMicrosoftMemoryCacheHandle("LinqCache");
#endif
            }), LifeStyles.Singleton);

            //object pool for linq 
            container.Register<IObjectPool<DynamicCodeCompiler>>(
                () =>
                    new ObjectPool<DynamicCodeCompiler>(20,
                        () => new DynamicCodeCompiler(container.GetInstance<ILogFactory>())), LifeStyles.Singleton);

            //created outside of the queue as part of setup, this must be a singleton.
            //all queues created from the setup class share the same message interceptors
            container.Register<IMessageInterceptorRegistrar, MessageInterceptors>(LifeStyles.Singleton);

            container.Register<MessageProcessingMode>(LifeStyles.Singleton);

            container.Register<IMessageFactory, MessageFactory>(LifeStyles.Singleton);
            container.Register<IMessageContextDataFactory, MessageContextDataFactory>(LifeStyles.Singleton);

            container.Register<IJobSchedulerMetaData, JobSchedulerMetaData>(LifeStyles.Singleton);

            container.Register<IQueueCancelWork, QueueCancelWork>(LifeStyles.Singleton);
            container.Register<ASerializer, RootSerializer>(LifeStyles.Singleton);
            container.Register<ISerializer, JsonSerializer>(LifeStyles.Singleton);
            container.Register<IExpressionSerializer, JsonExpressionSerializer>(LifeStyles.Singleton);
            container.Register<IQueueDelayFactory, QueueDelayFactory>(LifeStyles.Singleton);
            container.Register<ILinqCompiler, LinqCompiler>(LifeStyles.Singleton);

            container.Register<IInternalSerializer, JsonSerializerInternal>(LifeStyles.Singleton);
            container.Register<ICompositeSerialization, CompositeSerialization>(LifeStyles.Singleton);

            container.Register<IHeaders, Headers>(LifeStyles.Singleton);
            container.Register<IStandardHeaders, StandardHeaders>(LifeStyles.Singleton);
            container.Register<ICustomHeaders, CustomHeaders>(LifeStyles.Singleton);

            //because of it's usage in 'standard' modules, this must always be added, even if RPC is not enabled.
            //otherwise, the IoC container can't create the producer queue.
            container.Register<IRpcTimeoutFactory, RpcTimeoutFactory>(LifeStyles.Singleton);
            container.Register<IMessageMethodHandling, MessageMethodHandling>(LifeStyles.Singleton);

            container.Register<IRegisterMessagesAsync, RegisterMessagesAsync>(LifeStyles.Singleton);
            container.Register<IRegisterMessages, RegisterMessages>(LifeStyles.Singleton);

            container.Register<IMessageHandlerRegistration, MessageHandlerRegistration>(LifeStyles.Singleton);
            container
                .Register<IMessageHandlerRegistrationAsync, MessageHandlerRegistrationAsync>(LifeStyles.Singleton);

            container.Register<IGenerateReceivedMessage, GenerateReceivedMessage>(LifeStyles.Singleton);
            container.Register<IMetrics, MetricsNoOp>(LifeStyles.Singleton);

            //implementations required to send messages
            if ((registrationType & RegistrationTypes.Send) == RegistrationTypes.Send)
            {
                container.Register<ISentMessageFactory, SentMessageFactory>(LifeStyles.Singleton);
                container.Register<QueueConfigurationSend>(LifeStyles.Singleton);
                container.Register<QueueProducerConfiguration>(LifeStyles.Singleton);
                container.Register<TransportConfigurationSend>(LifeStyles.Singleton);
                container.Register<GenerateMessageHeaders>(LifeStyles.Singleton);
                container.Register<AddStandardMessageHeaders>(LifeStyles.Singleton);
                container.Register<IProducerMethodQueue, ProducerMethodQueue>(LifeStyles.Singleton);
                container.Register<IProducerMethodJobQueue, ProducerMethodJobQueue>(LifeStyles.Singleton);
            }

            //implementations for Receiving messages
            if ((registrationType & RegistrationTypes.Receive) == RegistrationTypes.Receive)
            {
                container.Register<TransportConfigurationReceive>(LifeStyles.Singleton);
                container.Register<IConsumerQueue, ConsumerQueue>(LifeStyles.Singleton);
                container.Register<IConsumerMethodQueue, ConsumerMethodQueue>(LifeStyles.Singleton);
                container.Register<IConsumerQueueAsync, ConsumerQueueAsync>(LifeStyles.Singleton);
                container.Register<IConsumerQueueScheduler, Scheduler>(LifeStyles.Singleton);
                container.Register<IConsumerMethodQueueScheduler, SchedulerMethod>(LifeStyles.Singleton);

                container.Register<QueueConsumerConfiguration>(LifeStyles.Singleton);

                container.Register<IHandleMessage, HandleMessage>(LifeStyles.Singleton);
                container.Register<IReceivedMessageFactory, ReceivedMessageFactory>(LifeStyles.Singleton);

                container.Register<IRetryDelayFactory, RetryDelayFactory>(LifeStyles.Singleton);
                container.Register<IRetryDelay, RetryDelay>(LifeStyles.Transient);

                container.Register<IWorkGroup, WorkGroupNoOp>(LifeStyles.Singleton);

                container.Register<IRetryInformationFactory, RetryInformationFactory>(LifeStyles.Singleton);

                container.Register<IWorkerWaitForEventOrCancel, WorkerWaitForEventOrCancel>(LifeStyles.Singleton);
                container.Register<IWaitForEventOrCancelThreadPool, WaitForEventOrCancelThreadPool>(LifeStyles.Singleton);

                container.Register<IMessageContext, MessageContext>(LifeStyles.Transient);
                container.Register<IMessageContextFactory, MessageContextFactory>(LifeStyles.Singleton);

                container.Register<IWorkerCollection, WorkerCollection>(LifeStyles.Singleton);
                container.Register<IWorker, Worker>(LifeStyles.Transient);

                container.Register<IPrimaryWorker, PrimaryWorker>(LifeStyles.Transient);
                container.Register<IPrimaryWorkerFactory, PrimaryWorkerFactory>(LifeStyles.Singleton);

                container.Register<IWorkerNameFactory, WorkerNameFactory>(LifeStyles.Singleton);
                container
                    .Register
                    <IWorkerHeartBeatNotificationFactory, WorkerHeartBeatNotificationFactory>(LifeStyles.Singleton);

                container.Register<IWorkerHeartBeatNotification, WorkerHeartBeatNotification>(LifeStyles.Transient);
                container.Register<ProcessMessage, ProcessMessage>(LifeStyles.Transient);
                container.Register<IMessageProcessingFactory, MessageProcessingFactory>(LifeStyles.Singleton);
                container.Register<MessageProcessing, MessageProcessing>(LifeStyles.Transient);

                container.Register<ITaskSchedulerConfiguration, TaskSchedulerConfiguration>(LifeStyles.Singleton);
                container.Register<ATaskScheduler, SmartThreadPoolTaskScheduler>(LifeStyles.Singleton);
                container.Register<SchedulerMessageHandler, SchedulerMessageHandler>(LifeStyles.Singleton);
                container.Register<ITaskFactory, SchedulerTaskFactory>(LifeStyles.Singleton);
                container.Register<ITaskSchedulerFactory, TaskSchedulerFactory>(LifeStyles.Singleton);
                container.Register<ITaskFactoryFactory, TaskFactoryFactory>(LifeStyles.Singleton);

                container.Register<IWorkerNotification, WorkerNotification>(LifeStyles.Transient);
                container.Register<IWorkerNotificationFactory, WorkerNotificationFactory>(LifeStyles.Singleton);

                container.Register<IAbortWorkerThread, AbortWorkerThread>(LifeStyles.Singleton);
                container.Register<StopThread>(LifeStyles.Singleton);
                container.Register<MessageExceptionHandler>(LifeStyles.Singleton);
                container.Register<MessageProcessingAsync>(LifeStyles.Transient);
                container.Register<ProcessMessageAsync>(LifeStyles.Singleton);

                container.Register<IClearExpiredMessagesMonitor, ClearExpiredMessagesMonitor>(LifeStyles.Singleton);

                container.Register<IHeartBeatScheduler, HeartBeatScheduler>(LifeStyles.Singleton);

                container.Register<IHeartBeatWorkerFactory, HeartBeatWorkerFactory>(LifeStyles.Singleton);
                container.Register<IQueueWaitFactory, QueueWaitFactory>(LifeStyles.Singleton);
                container.Register<IHeartBeatMonitor, HeartBeatMonitor>(LifeStyles.Singleton);
                container.Register<IQueueMonitor, QueueMonitor>(LifeStyles.Singleton);

                container.Register<HeartBeatMonitorNoOp>(LifeStyles.Singleton);
                container.Register<ClearExpiredMessagesMonitorNoOp>(LifeStyles.Singleton);

                container.Register<IWorkerConfiguration, WorkerConfiguration>(LifeStyles.Singleton);
                container.Register<IHeartBeatConfiguration, HeartBeatConfiguration>(LifeStyles.Singleton);
                container.Register<IHeartBeatThreadPoolConfiguration, HeartBeatThreadPoolConfiguration>(
                    LifeStyles.Singleton);
                container.Register<IMessageExpirationConfiguration, MessageExpirationConfiguration>(LifeStyles.Singleton);


                container.Register<IMessageHandler, MessageHandler>(LifeStyles.Singleton);

                container.Register<IMessageHandlerAsync, MessageHandlerAsync>(LifeStyles.Singleton);

                container.Register<IWorkerFactory, WorkerFactory>(LifeStyles.Singleton);
                container.Register<StopWorker>(LifeStyles.Singleton);

                container.Register<ICommitMessage, CommitMessage>(LifeStyles.Singleton);
                container.Register<IRollbackMessage, RollbackMessage>(LifeStyles.Singleton);
                container.Register<WaitForThreadToFinish>(LifeStyles.Singleton);
                container.Register<WorkerTerminate>(LifeStyles.Singleton);
                container.Register<IWaitForEventOrCancelWorker, WaitForEventOrCancelWorker>(LifeStyles.Singleton);
                container.Register<IWaitForEventOrCancelFactory, WaitForEventOrCancelFactory>(LifeStyles.Singleton);
                container.Register<ThreadPoolConfiguration>(LifeStyles.Singleton);
            }

            //implementations for RPC / or a duplex transport
            //NOTE - we don't bother to tell the difference between RPC / duplex
            //so it's possible these are registered, but never actually used.
            if ((registrationType & RegistrationTypes.Receive) == RegistrationTypes.Receive &&
                (registrationType & RegistrationTypes.Send) == RegistrationTypes.Send)
            {
                container.Register<IRpcMethodQueue, RpcMethodQueue>(LifeStyles.Singleton);

                container.Register<IClearExpiredMessagesRpcMonitor, ClearExpiredMessagesRpcMonitor>
                    (LifeStyles.Singleton);

                container.Register<IResponseIdFactory, ResponseIdFactory>(LifeStyles.Singleton);
                container.Register<IRpcContextFactory, RpcContextFactory>(LifeStyles.Singleton);
                container.Register<QueueRpcConfiguration>(LifeStyles.Singleton);
            }
        }

        private static void RegisterSharedDefaults(IContainer container)
        {
#region Singletons

            container.Register<IContainerFactory, ContainerFactory>(LifeStyles.Singleton);

            //register the generic configuration container
            container.Register<IConfiguration, AdditionalConfiguration>(LifeStyles.Singleton);
#endregion

#region Logging

#if (DEBUG)
            container.Register<ILogProvider, ColoredConsoleLogProvider>(LifeStyles.Singleton);
#else
            container.Register<ILogProvider,NoSpecifiedLogProvider>( LifeStyles.Singleton);
#endif
            container.Register<ILogFactory, LogFactory>(LifeStyles.Singleton);

#endregion

            container.Register<IQueueStatus, QueueStatusHttp>(LifeStyles.Singleton);
            container.Register<BaseTimeConfiguration>(LifeStyles.Singleton);
            container.Register<IGetTimeFactory, GetTimeFactory>(LifeStyles.Singleton);
            container.Register<IGetTime, LocalMachineTime>(LifeStyles.Singleton);

            container.Register<QueueStatusHttpConfiguration>(LifeStyles.Singleton);
            container.Register<IQueueStatusProvider, QueueStatusProviderNoOp>(LifeStyles.Singleton);

            container.Register<IInterceptorFactory, InterceptorFactory>(LifeStyles.Singleton);
            container.RegisterCollection<IMessageInterceptor>(Enumerable.Empty<Type>());

            container.Register<IPolicies, Policies.Policies>(LifeStyles.Singleton);
            container.Register<PolicyRegistry>(LifeStyles.Singleton);
            container.Register<PolicyDefinitions>(LifeStyles.Singleton);

            RegisterMetricDecorators(container);
            RegisterPolicyDecorators(container);
            RegisterLoggerDecorators(container);

            //register the linq cache decorator
            container.RegisterDecorator<ILinqCompiler, LinqCompileCacheDecorator>(LifeStyles.Singleton);
        }
        /// <summary>
        /// Suppress warnings for specific cases.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="registrationType">Type of the registration.</param>
        public static void SuppressWarningsIfNeeded(IContainer container, RegistrationTypes registrationType)
        {
            if ((registrationType & RegistrationTypes.Receive) == RegistrationTypes.Receive)
            {
                container.SuppressDiagnosticWarning(typeof(IMessageContext),
                    DiagnosticTypes.DisposableTransientComponent,
                    "IMessageContext is explicitly disposed of via a using statement");

                container.SuppressDiagnosticWarning(typeof(MessageContext),
                  DiagnosticTypes.DisposableTransientComponent,
                  "MessageContext is explicitly disposed of via a using statement");

                container.SuppressDiagnosticWarning(typeof(ATaskScheduler),
                    DiagnosticTypes.DisposableTransientComponent,
                    "ATaskScheduler is disposed of via its parent queue if created by this library. Otherwise, the caller of this library is responsible for disposing the task scheduler");

                container.SuppressDiagnosticWarning(typeof(IWorker),
                   DiagnosticTypes.DisposableTransientComponent,
                    "IWorker is disposed of via the worker collection");

                container.SuppressDiagnosticWarning(typeof(IPrimaryWorker),
                   DiagnosticTypes.DisposableTransientComponent,
                   "IPrimaryWorker is disposed of via the queue");
            }
        }

        /// <summary>
        /// Setup the default policies.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="registrationType">Type of the registration.</param>
        public static void SetupDefaultPolicies(IContainer container, RegistrationTypes registrationType)
        {
            var policies = container.GetInstance<IPolicies>();
            var noOp = Policy.NoOp(); //thread safe - can be re-used
            var noOpAsync = Policy.NoOpAsync();

            //ReceiveMessageFromTransport
            policies.Registry[policies.Definition.ReceiveMessageFromTransport] = noOp;
            //SendHeartBeat
            policies.Registry[policies.Definition.SendHeartBeat] = noOp;
            //SendMessage
            policies.Registry[policies.Definition.SendMessage] = noOp;


            //ReceiveMessageFromTransportASync
            policies.Registry[policies.Definition.ReceiveMessageFromTransportAsync] = noOpAsync;
            //SendHeartBeatAsync
            policies.Registry[policies.Definition.SendHeartBeatAsync] = noOpAsync;
            //SendMessageAsync
            policies.Registry[policies.Definition.SendMessageAsync] = noOpAsync;
        }

        /// <summary>
        /// Registers the fall backs for generic types
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="registrationType">Type of the registration.</param>
        public static void RegisterFallbacks(IContainer container, RegistrationTypes registrationType)
        {
            container.RegisterConditional(typeof(ICachePolicy<>), typeof(CachePolicy<>), LifeStyles.Singleton);

            if ((registrationType & RegistrationTypes.Send) == RegistrationTypes.Send)
            {
                container.RegisterConditional(typeof (IProducerQueue<>), typeof (ProducerQueue<>),
                    LifeStyles.Singleton);
            }

            if ((registrationType & RegistrationTypes.Receive) == RegistrationTypes.Receive &&
            (registrationType & RegistrationTypes.Send) == RegistrationTypes.Send)
            {
                container.RegisterConditional(typeof(IProducerQueueRpc<>), typeof(ProducerQueueRpc<>), LifeStyles.Singleton);
                container.RegisterConditional(typeof(IRpcQueue<,>), typeof(RpcQueue<,>), LifeStyles.Singleton);
 

                container.RegisterConditional(typeof(IMessageProcessingRpcSend<>), typeof(MessageProcessingRpcSend<>), LifeStyles.Singleton);
                container.RegisterConditional(typeof(IMessageProcessingRpcReceive<>), typeof(MessageProcessingRpcReceive<>), LifeStyles.Singleton);
            }
        }

        /// <summary>
        /// Registers the logger decorators.
        /// </summary>
        /// <param name="container">The container.</param>
        private static void RegisterLoggerDecorators(IContainer container)
        {
            container.RegisterDecorator<IAbortWorkerThread, AbortWorkerThreadDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IRollbackMessage, RollbackMessageDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IClearExpiredMessages, ClearExpiredMessagesDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IResetHeartBeat, ResetHeartBeatDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IReceivePoisonMessage, ReceivePoisonMessageDecorator>(LifeStyles.Singleton);
        }

        private static void RegisterPolicyDecorators(IContainer container)
        {
            container.RegisterDecorator<IReceiveMessages, ReceiveMessagesPolicyDecorator>(LifeStyles.Transient);
        }

        /// <summary>
        /// Registers the decorator metrics.
        /// </summary>
        /// <param name="container">The container.</param>
        private static void RegisterMetricDecorators(IContainer container)
        {
            //common decorators for metrics
            container.RegisterDecorator<ISerializer, SerializerDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IQueueCreation, QueueCreationDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IMessageHandler, MessageHandlerDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IMessageHandlerAsync, MessageHandlerAsyncDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IResetHeartBeat, Metrics.Decorator.ResetHeartBeatDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IInternalSerializer, InternalSerializerDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<ISendHeartBeat, SendHeartBeatDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<ISendMessages, SendMessagesDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IReceiveMessages, ReceiveMessagesDecorator>(LifeStyles.Transient);
            container.RegisterDecorator<IReceiveMessagesError, ReceiveMessagesErrorDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IReceivePoisonMessage, Metrics.Decorator.ReceivePoisonMessageDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<ICommitMessage, CommitMessageDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IRollbackMessage, RollbackMessageDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IClearExpiredMessages, Metrics.Decorator.ClearExpiredMessagesDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IExpressionSerializer, ExpressionSerializerDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IMessageMethodHandling, MessageMethodHandlingDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<ILinqCompiler, LinqCompilerDecorator>(LifeStyles.Singleton);

            //while this is registered as transient, it ends up being a singleton because the interceptor host is a singleton
            container.RegisterDecorator<IMessageInterceptor, MessageInterceptorDecorator>(LifeStyles.Transient);
        }
    }
    /// <summary>
    /// Registration type for the root container
    /// </summary>
    [Flags]
    public enum RegistrationTypes
    {
        /// <summary>
        /// Standard classes used by everything. Always added.
        /// </summary>
        Default = 0,
        /// <summary>
        /// Classes used to send messages.
        /// </summary>
        Send = 1,
        /// <summary>
        /// Classes used to receive messages.
        /// </summary>
        Receive = 2
    }
}