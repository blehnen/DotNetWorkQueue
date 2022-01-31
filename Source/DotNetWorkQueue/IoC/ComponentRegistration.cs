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
using System.Diagnostics;
using System.Linq;
using DotNetWorkQueue.Cache;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Factory;
using DotNetWorkQueue.Interceptors;
using DotNetWorkQueue.JobScheduler;
using DotNetWorkQueue.LinqCompile;
using DotNetWorkQueue.LinqCompile.Decorator;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Metrics.NoOp;
using DotNetWorkQueue.Policies;
using DotNetWorkQueue.Policies.Decorator;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.TaskScheduling;
using DotNetWorkQueue.Time;
using DotNetWorkQueue.Transport.Memory.Basic;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenTelemetry.Trace;
using Polly;
using Polly.Caching.Memory;
using Polly.Registry;
using SimpleInjector;

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
        /// <param name="connection">the queue connection information.</param>
        public static void RegisterDefaultsForScheduler(IContainer container, QueueConnection connection)
        {
            Guard.NotNull(() => container, container);

            RegisterSharedDefaults(container, connection);

            container.Register<ITaskSchedulerConfiguration, TaskSchedulerConfiguration>(LifeStyles.Singleton);

            container.Register<ATaskScheduler, SmartThreadPoolTaskScheduler>(LifeStyles.Singleton);
            container.AddTypeThatNeedsWarningSuppression(typeof(ATaskScheduler));

            container.Register<ITaskFactory, SchedulerTaskFactory>(LifeStyles.Singleton);
            container.Register<IWaitForEventOrCancelThreadPool, WaitForEventOrCancelThreadPool>(LifeStyles.Singleton);
            container.Register<IWaitForEventOrCancelFactory, WaitForEventOrCancelFactory>(LifeStyles.Singleton);
            container.Register<ISchedulerMessageHandler, SchedulerMessageHandler>(LifeStyles.Singleton);
            container.Register<ITaskSchedulerFactory, TaskSchedulerFactory>(LifeStyles.Singleton);
            container.Register<ITaskFactoryFactory, TaskFactoryFactory>(LifeStyles.Singleton);
            container.Register<IMetrics, MetricsNoOp>(LifeStyles.Singleton);
        }

        /// <summary>
        /// Registers the defaults implementations.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="connection">the queue connection information.</param>
        public static void RegisterDefaultsForJobScheduler(IContainer container, QueueConnection connection)
        {
            Guard.NotNull(() => container, container);
            RegisterSharedDefaults(container, connection);
            container.Register<IMetrics, MetricsNoOp>(LifeStyles.Singleton);
        }

        /// <summary>
        /// Registers the defaults implementations.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="registrationType">Type of the registration.</param>
        /// <param name="connection">the queue connection information.</param>
        public static void RegisterDefaults(IContainer container,
            RegistrationTypes registrationType, QueueConnection connection)
        {
            Guard.NotNull(() => container, container);

            //default types that are always registered in the container for both send and receive
            RegisterSharedDefaults(container, connection);

            //object cache
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var memoryCacheProvider = new MemoryCacheProvider(memoryCache);
            container.Register(() => memoryCacheProvider, LifeStyles.Singleton);

            //object pool for linq 
            container.Register<IObjectPool<DynamicCodeCompiler>>(
                () =>
                    new ObjectPool<DynamicCodeCompiler>(20,
                        () => new DynamicCodeCompiler(container.GetInstance<ILogger>())), LifeStyles.Singleton);

            //created outside of the queue as part of setup, this must be a singleton.
            //all queues created from the setup class share the same message interceptors
            container.Register<IMessageInterceptorRegistrar, MessageInterceptors>(LifeStyles.Singleton);

            container.Register<MessageProcessingMode>(LifeStyles.Singleton);
            container.Register<IGetPreviousMessageErrors, GetPreviousMessageErrorsNoOp>(LifeStyles.Singleton);

            container.Register<IMessageFactory, MessageFactory>(LifeStyles.Singleton);


            container.Register<IJobSchedulerMetaData, JobSchedulerMetaData>(LifeStyles.Singleton);

            container.Register<IQueueCancelWork, QueueCancelWork>(LifeStyles.Singleton);
            container.Register<ASerializer, RootSerializer>(LifeStyles.Singleton);
            container.Register<ISerializer, JsonSerializer>(LifeStyles.Singleton);
            container.Register<IExpressionSerializer, JsonExpressionSerializer>(LifeStyles.Singleton);
            container.Register<IQueueDelayFactory, QueueDelayFactory>(LifeStyles.Singleton);
            container.Register<ILinqCompiler, LinqCompiler>(LifeStyles.Singleton);
            container.Register<IGetHeader, GetHeaderDefault>(LifeStyles.Singleton);

            container.Register<IInternalSerializer, JsonSerializerInternal>(LifeStyles.Singleton);
            container.Register<ICompositeSerialization, CompositeSerialization>(LifeStyles.Singleton);

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
                container.AddTypeThatNeedsWarningSuppression(typeof(IMessageContext));
                container.AddTypeThatNeedsWarningSuppression(typeof(MessageContext));

                container.Register<IMessageContextFactory, MessageContextFactory>(LifeStyles.Singleton);

                container.Register<IWorkerCollection, WorkerCollection>(LifeStyles.Singleton);

                container.Register<IWorker, Worker>(LifeStyles.Transient);
                container.AddTypeThatNeedsWarningSuppression(typeof(IWorker));

                container.Register<IPrimaryWorker, PrimaryWorker>(LifeStyles.Transient);
                container.AddTypeThatNeedsWarningSuppression(typeof(IPrimaryWorker));

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
                container.Register<ISchedulerMessageHandler, SchedulerMessageHandler>(LifeStyles.Singleton);
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
                container.Register<IClearErrorMessagesMonitor, ClearErrorMessagesMonitor>(LifeStyles.Singleton);

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
                container.Register<IMessageErrorConfiguration, MessageErrorConfiguration>(LifeStyles.Singleton);

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
        }

        private static void RegisterSharedDefaults(IContainer container, QueueConnection connection)
        {
            #region Singletons

            container.Register<IContainerFactory, ContainerFactory>(LifeStyles.Singleton);

            //register the generic configuration container
            container.Register<IConfiguration, AdditionalConfiguration>(LifeStyles.Singleton);
            #endregion

            #region Logging
            container.Register<ILoggerFactory>(() => NullLoggerFactory.Instance, LifeStyles.Singleton);
            container.Register<ILogger>(() =>
            {
                var factory = container.GetInstance<ILoggerFactory>();
                return factory.CreateLogger(connection == null ? "null" : connection.Queue);
            }, LifeStyles.Singleton);
            #endregion

            #region Open Tracing

            var tracer = new ActivitySource(
                "dotnetworkqueue.instrumentationlibrary");
            container.Register<ActivitySource>(() => tracer, LifeStyles.Singleton);
            #endregion
            container.Register<BaseTimeConfiguration>(LifeStyles.Singleton);
            container.Register<IGetTimeFactory, GetTimeFactory>(LifeStyles.Singleton);
            container.Register<IGetTime, LocalMachineTime>(LifeStyles.Singleton);

            container.Register<IInterceptorFactory, InterceptorFactory>(LifeStyles.Singleton);
            container.RegisterCollection<IMessageInterceptor>(Enumerable.Empty<Type>());

            container.Register<IPolicies, Policies.Policies>(LifeStyles.Singleton);
            container.Register<PolicyRegistry>(LifeStyles.Singleton);
            container.Register<PolicyDefinitions>(LifeStyles.Singleton);

            //because of it's usage in 'standard' modules, this must always be added.
            //otherwise, the IoC container can't create the producer queue.
            container.Register<IMessageContextDataFactory, MessageContextDataFactory>(LifeStyles.Singleton);

            container.Register<IHeaders, Headers>(LifeStyles.Singleton);
            container.Register<IStandardHeaders, StandardHeaders>(LifeStyles.Singleton);
            container.Register<ICustomHeaders, CustomHeaders>(LifeStyles.Singleton);

            RegisterMetricDecorators(container);
            RegisterPolicyDecorators(container);
            RegisterLoggerDecorators(container);
            RegisterTraceDecorators(container);

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
                container.RegisterConditional(typeof(IProducerQueue<>), typeof(ProducerQueue<>),
                    LifeStyles.Singleton);
            }
        }

        /// <summary>
        /// Registers the logger decorators.
        /// </summary>
        /// <param name="container">The container.</param>
        private static void RegisterLoggerDecorators(IContainer container)
        {
            container.RegisterDecorator<IAbortWorkerThread, Logging.Decorator.AbortWorkerThreadDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IRollbackMessage, Logging.Decorator.RollbackMessageDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IClearExpiredMessages, Logging.Decorator.ClearExpiredMessagesDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IClearErrorMessages, Logging.Decorator.ClearErrorMessagesDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IResetHeartBeat, Logging.Decorator.ResetHeartBeatDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IReceivePoisonMessage, Logging.Decorator.ReceivePoisonMessageDecorator>(LifeStyles.Singleton);
        }

        private static void RegisterPolicyDecorators(IContainer container)
        {
            container.RegisterDecorator<IReceiveMessages, ReceiveMessagesPolicyDecorator>(LifeStyles.Transient);
            container.RegisterDecorator<ISendHeartBeat, SendHeartBeatPolicyDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<ISendMessages, SendMessagesPolicyDecorator>(LifeStyles.Singleton);
        }

        private static void RegisterTraceDecorators(IContainer container)
        {
            container.RegisterDecorator<IReceiveMessages, Trace.Decorator.ReceiveMessagesDecorator>(LifeStyles.Transient);
            container.RegisterDecorator<IMessageHandler, Trace.Decorator.MessageHandlerDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IMessageHandlerAsync, Trace.Decorator.MessageHandlerAsyncDecorator>(
                LifeStyles.Singleton);
            container.RegisterDecorator<ICommitMessage, Trace.Decorator.CommitMessageDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IRemoveMessage, Trace.Decorator.RemoveMessageDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IMessageMethodHandling, Trace.Decorator.MessageMethodHandlingDecorator>(
                LifeStyles.Singleton);
            container.RegisterDecorator<IReceiveMessagesError, Trace.Decorator.ReceiveMessagesErrorDecorator>(
                LifeStyles.Singleton);
            container.RegisterDecorator<IReceivePoisonMessage, Trace.Decorator.ReceivePoisonMessageDecorator>(
                LifeStyles.Singleton);
            container.RegisterDecorator<IResetHeartBeat, Trace.Decorator.ResetHeartBeatDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IRollbackMessage, Trace.Decorator.RollbackMessageDecorator>(
                LifeStyles.Singleton);
            container.RegisterDecorator<IMessageInterceptor, Trace.Decorator.MessageInterceptorDecorator>(LifeStyles
                .Transient);

            container.RegisterDecorator<ISerializer, Trace.Decorator.SerializerDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<ISendHeartBeat, Trace.Decorator.SendHeartBeatDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IProducerMethodJobQueue, Trace.Decorator.ProducerMethodJobQueueDecorator>(
                LifeStyles.Singleton);

            container.RegisterDecorator<ISchedulerMessageHandler, Trace.Decorator.SchedulerMessageHandlerDecorator>(
                LifeStyles.Singleton);
        }

        /// <summary>
        /// Registers the decorator metrics.
        /// </summary>
        /// <param name="container">The container.</param>
        private static void RegisterMetricDecorators(IContainer container)
        {
            //common decorators for metrics
            container.RegisterDecorator<ISerializer, Metrics.Decorator.SerializerDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IQueueCreation, Metrics.Decorator.QueueCreationDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IMessageHandler, Metrics.Decorator.MessageHandlerDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IMessageHandlerAsync, Metrics.Decorator.MessageHandlerAsyncDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IResetHeartBeat, Metrics.Decorator.ResetHeartBeatDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IInternalSerializer, Metrics.Decorator.InternalSerializerDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<ISendHeartBeat, Metrics.Decorator.SendHeartBeatDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<ISendMessages, Metrics.Decorator.SendMessagesDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IReceiveMessages, Metrics.Decorator.ReceiveMessagesDecorator>(LifeStyles.Transient);
            container.RegisterDecorator<IReceiveMessagesError, Metrics.Decorator.ReceiveMessagesErrorDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IReceivePoisonMessage, Metrics.Decorator.ReceivePoisonMessageDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<ICommitMessage, Metrics.Decorator.CommitMessageDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IRollbackMessage, Metrics.Decorator.RollbackMessageDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IClearExpiredMessages, Metrics.Decorator.ClearExpiredMessagesDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IClearErrorMessages, Metrics.Decorator.ClearErrorMessagesDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IExpressionSerializer, Metrics.Decorator.ExpressionSerializerDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IMessageMethodHandling, Metrics.Decorator.MessageMethodHandlingDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<ILinqCompiler, Metrics.Decorator.LinqCompilerDecorator>(LifeStyles.Singleton);

            //while this is registered as transient, it ends up being a singleton because the interceptor host is a singleton
            container.RegisterDecorator<IMessageInterceptor, Metrics.Decorator.MessageInterceptorDecorator>(LifeStyles.Transient);
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