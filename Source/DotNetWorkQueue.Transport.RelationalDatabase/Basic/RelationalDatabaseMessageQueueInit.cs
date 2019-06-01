// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
using System.Reflection;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Factory;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <summary>
    /// Sets default implementions in the container
    /// </summary>
    public class RelationalDatabaseMessageQueueInit
    {
        /// <summary>
        /// Registers the standard implementations.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="caller">The caller.</param>
        public void RegisterStandardImplementations(IContainer container, params Assembly[] caller)
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
            container.Register<IRemoveMessage, RemoveMessage>(LifeStyles.Singleton);
            container.Register<IGetHeader, GetHeader>(LifeStyles.Singleton);
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

        /// <summary>
        /// Sets the defaults if needed.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="configurationSendName">Name of the configuration send.</param>
        /// <param name="configurationReceiveName">Name of the configuration receive.</param>
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

            transportReceive.MessageExpirationSupported = options.EnableMessageExpiration;

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
            heartBeatConfiguration.MonitorTime = TimeSpan.FromMinutes(3);
            heartBeatConfiguration.UpdateTime = "min(*%2)";
            heartBeatConfiguration.ThreadPoolConfiguration.ThreadsMax = 1;
            heartBeatConfiguration.ThreadPoolConfiguration.WaitForThreadPoolToFinish = TimeSpan.FromSeconds(5);
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

        private void RegisterCommands(IContainer container, params Assembly[] target)
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
