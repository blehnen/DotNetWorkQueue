using System;
using System.Reflection;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Factory;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    public class MessageQueueInit
    {
        public void RegisterStandardImplementations(IContainer container, Assembly caller)
        {
            Guard.NotNull(() => container, container);

            //**all
            container.Register<QueueStatusQueries>(LifeStyles.Singleton);
            container.Register<IQueueStatusProvider, QueueStatusProvider>(LifeStyles.Singleton);
            container.Register<IJobTableCreation, JobTableCreation>(LifeStyles.Singleton);
            container.Register<CreateJobMetaData>(LifeStyles.Singleton);
            container.Register<IReadColumn, ReadColumn>(LifeStyles.Singleton);
            container.Register<ITransactionFactory, TransactionFactory>(LifeStyles.Singleton);
            container.Register<ICreationScope, CreationScopeNoOp>(LifeStyles.Singleton);
            container.Register<ICorrelationIdFactory, CorrelationIdFactory>(
                LifeStyles.Singleton);
            container.Register<TableNameHelper>(LifeStyles.Singleton);
            container.Register<IClearExpiredMessages, ClearExpiredMessages>(LifeStyles.Singleton);
            //**all

            //**send
            container.Register<ISendMessages, SendMessages>(LifeStyles.Singleton);
            //**send


            //**receive
            container.Register<IGetColumnsFromTable, GetColumnsFromTable>(LifeStyles.Singleton);
            container.Register<IResetHeartBeat, ResetHeartBeat>(LifeStyles.Singleton);
            container.Register<ISendHeartBeat, SendHeartBeat>(LifeStyles.Singleton);
            container.Register<IReceiveMessagesFactory, ReceiveMessagesFactory>(LifeStyles.Singleton);
            container.Register<IReceivePoisonMessage, ReceivePoisonMessage>(LifeStyles.Singleton);
            container.Register<IReceiveMessagesError, ReceiveErrorMessage>(LifeStyles.Singleton);
            container.Register<IIncreaseQueueDelay, IncreaseQueueDelay>(LifeStyles.Singleton);
            //**receive

            RegisterCommands(container, caller);
            RegisterCommands(container, Assembly.GetAssembly(GetType()));
        }

        public void SetDefaultsIfNeeded(IContainer container, string configurationSendName, string configurationReceiveName)
        {
            var factory = container.GetInstance<ITransportOptionsFactory>();
            var options = factory.Create();
            var configurationSend = container.GetInstance<QueueProducerConfiguration>();
            var configurationReceive = container.GetInstance<QueueConsumerConfiguration>();

            configurationSend.AdditionalConfiguration.SetSetting(configurationSendName, options);
            configurationReceive.AdditionalConfiguration.SetSetting(configurationReceiveName, options);

            var transportReceive = container.GetInstance<TransportConfigurationReceive>();


            transportReceive.HeartBeatSupported = options.EnableHeartBeat && options.EnableStatus &&
                                                  !options.EnableHoldTransactionUntilMessageCommitted;

            transportReceive.MessageExpirationSupported = options.EnableMessageExpiration ||
                                                          options.QueueType == QueueTypes.RpcReceive ||
                                                          options.QueueType == QueueTypes.RpcSend;

            transportReceive.MessageRollbackSupported = options.EnableStatus ||
                                                        options.EnableHoldTransactionUntilMessageCommitted;

            transportReceive.QueueDelayBehavior.Clear();
            transportReceive.QueueDelayBehavior.Add(DefaultQueueDelay.GetDefaultQueueDelay());
            transportReceive.FatalExceptionDelayBehavior.Clear();
            transportReceive.FatalExceptionDelayBehavior.Add(ExceptionDelay.GetExceptionDelay());

            transportReceive.LockFeatures();

            SetupHeartBeat(container);
            SetupMessageExpiration(container);
        }

        /// <summary>
        /// Setup the heart beat.
        /// </summary>
        /// <param name="container">The container.</param>
        public void SetupHeartBeat(IContainer container)
        {
            var heartBeatConfiguration = container.GetInstance<IHeartBeatConfiguration>();
            if (!heartBeatConfiguration.Enabled) return;
            heartBeatConfiguration.Time = TimeSpan.FromSeconds(600);
            heartBeatConfiguration.MonitorTime = TimeSpan.FromSeconds(120);
            heartBeatConfiguration.Interval = 4;
            heartBeatConfiguration.ThreadPoolConfiguration.ThreadsMax = 1;
            heartBeatConfiguration.ThreadPoolConfiguration.ThreadsMin = 1;
            heartBeatConfiguration.ThreadPoolConfiguration.ThreadIdleTimeout = TimeSpan.FromSeconds(220);
        }

        /// <summary>
        /// Setup the message expiration.
        /// </summary>
        /// <param name="container">The container.</param>
        public void SetupMessageExpiration(IContainer container)
        {
            var configuration = container.GetInstance<IMessageExpirationConfiguration>();
            if (!configuration.Supported) return;
            configuration.MonitorTime = TimeSpan.FromMinutes(3);
            configuration.Enabled = true;
        }

        private void RegisterCommands(IContainer container, Assembly target)
        {
            //commands and decorators
            // Go look in all assemblies and register all implementations
            // of ICommandHandlerWithOutput<T> by their closed interface:
            container.Register(typeof(ICommandHandlerWithOutput<,>), LifeStyles.Singleton,
                target);

            //commands and decorators
            // Go look in all assemblies and register all implementations
            // of ICommandHandlerWithOutputAsync<T> by their closed interface:
            container.Register(typeof(ICommandHandlerWithOutputAsync<,>), LifeStyles.Singleton,
                target);

            // Go look in all assemblies and register all implementations
            // of ICommandHandler<T> by their closed interface:
            container.Register(typeof(ICommandHandler<>), LifeStyles.Singleton,
                target);

            // Go look in all assemblies and register all implementations
            // of IQueryHandler<T> by their closed interface:
            container.Register(typeof(IQueryHandler<,>), LifeStyles.Singleton,
                target);

            // Go look in all assemblies and register all implementations
            // of ICommandHandler<T> by their closed interface:
            container.Register(typeof(IPrepareCommandHandler<>), LifeStyles.Singleton,
                target);

            // Go look in all assemblies and register all implementations
            // of ICommandHandler<T> by their closed interface:
            container.Register(typeof(IPrepareCommandHandlerWithOutput<,>), LifeStyles.Singleton,
                target);

            // Go look in all assemblies and register all implementations
            // of ICommandHandler<T> by their closed interface:
            container.Register(typeof(IPrepareQueryHandler<,>), LifeStyles.Singleton,
                target);
        }
    }
}
