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
using System.Collections.Generic;
using System.Reflection;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Transport.PostgreSQL.Basic.Factory;
using DotNetWorkQueue.Transport.PostgreSQL.Basic.Message;
using DotNetWorkQueue.Transport.PostgreSQL.Basic.Time;
using DotNetWorkQueue.Transport.PostgreSQL.Decorator;
using CommitMessage = DotNetWorkQueue.Transport.PostgreSQL.Basic.Message.CommitMessage;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.PostgreSQL.Schema;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <summary>
    /// Registers the implementations for the queue into the IoC container.
    /// </summary>
    public class PostgreSqlMessageQueueInit : TransportInitDuplex
    {
        /// <summary>
        /// Registers the implementations.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="registrationType">Type of the registration.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="queue">The queue.</param>
        public override void RegisterImplementations(IContainer container, RegistrationTypes registrationType,
            string connection, string queue)
        {
            Guard.NotNull(() => container, container);

            //**all
            container.Register<IDbConnectionFactory, DbConnectionFactory>(LifeStyles.Singleton);
            container.Register<PostgreSqlMessageQueueSchema>(LifeStyles.Singleton);
            container.Register<IQueueCreation, PostgreSqlMessageQueueCreation>(LifeStyles.Singleton);
            container.Register<PostgreSqlMessageQueueStatusQueries>(LifeStyles.Singleton);
            container.Register<IQueueStatusProvider, PostgreSqlQueueStatusProvider>(LifeStyles.Singleton);
            container.Register<IJobSchedulerLastKnownEvent, PostgreSqlJobSchedulerLastKnownEvent>(LifeStyles.Singleton);
            container.Register<IJobTableCreation, PostgreSqlJobTableCreation>(LifeStyles.Singleton);
            container.Register<IOptionsSerialization, OptionsSerialization>(LifeStyles.Singleton);
            container.Register<PostgreSqlJobSchema>(LifeStyles.Singleton);
            container.Register<ISendJobToQueue, PostgreSqlSendJobToQueue>(LifeStyles.Singleton);
            container.Register<CreateJobMetaData>(LifeStyles.Singleton);
            container.Register<CommandStringCache, PostgreSqlCommandStringCache>(LifeStyles.Singleton);

            container.Register<IGetTime, PostgreSqlTime>(LifeStyles.Singleton);
            container.Register<IGetFirstMessageDeliveryTime, GetFirstMessageDeliveryTime>(LifeStyles.Singleton);
            container
                .Register
                <IPostgreSqlMessageQueueTransportOptionsFactory, PostgreSqlMessageQueueTransportOptionsFactory>(
                    LifeStyles.Singleton);

            container.Register<ICreationScope, CreationScopeNoOp>(LifeStyles.Singleton);
            container.Register<PostgreSqlCommandStringCache>(LifeStyles.Singleton);

            container.Register<IConnectionInformation>(() => new SqlConnectionInformation(queue, connection),
                LifeStyles.Singleton);
            container.Register<ICorrelationIdFactory, PostgreSqlMessageQueueCorrelationIdFactory>(
                LifeStyles.Singleton);

            container.Register<PostgreSqlMessageQueueTransportOptions>(LifeStyles.Singleton);

            container.Register<TableNameHelper>(LifeStyles.Singleton);
            container.Register<SqlHeaders>(LifeStyles.Singleton);

            container.Register<ThreadSafeRandom>(LifeStyles.Singleton);
            container.Register<IClearExpiredMessages, PostgreSqlMessageQueueClearExpiredMessages>(LifeStyles.Singleton);
            //**all

            //**send
            container.Register<ISendMessages, PostgreSqlMessageQueueSend>(LifeStyles.Singleton);
            //**send


            //**receive
            container.Register<IReceiveMessages, PostgreSqlMessageQueueReceive>(LifeStyles.Transient);
            container.Register<IConnectionFactory, ConnectionFactory>(LifeStyles.Singleton);
            container.Register<CommitMessage>(LifeStyles.Transient);
            container.Register<RollbackMessage>(LifeStyles.Transient);
            container.Register<HandleMessage>(LifeStyles.Transient);
            container.Register<ReceiveMessage>(LifeStyles.Transient);

            container.Register<IResetHeartBeat, PostgreSqlMessageQueueResetHeartBeat>(LifeStyles.Singleton);
            container.Register<ISendHeartBeat, PostgreSqlMessageQueueSendHeartBeat>(LifeStyles.Singleton);
            container.Register<IReceiveMessagesFactory, ReceiveMessagesFactory>(LifeStyles.Singleton);
            container.Register<IReceivePoisonMessage, PostgreSqlQueueReceivePoisonMessage>(LifeStyles.Singleton);
            container.Register<IReceiveMessagesError, PostgreSqlMessageQueueReceiveErrorMessage>(LifeStyles.Singleton);
            //**receive

            var target = Assembly.GetAssembly(GetType());
            RegisterCommands(container, target);

            var target2 = Assembly.GetAssembly(typeof(ITable));
            if(target.FullName != target2.FullName)
                RegisterCommands(container, target2);

            container.RegisterDecorator(typeof(ICommandHandlerWithOutput<,>),
                typeof(RetryCommandHandlerOutputDecorator<,>), LifeStyles.Singleton);

            container.RegisterDecorator(typeof(ICommandHandler<>),
                typeof(RetryCommandHandlerDecorator<>), LifeStyles.Singleton);

            container.RegisterDecorator(typeof(ICommandHandlerWithOutputAsync<,>),
                typeof(RetryCommandHandlerOutputDecoratorAsync<,>), LifeStyles.Singleton);

            container.RegisterDecorator(typeof(IQueryHandler<,>),
                typeof(RetryQueryHandlerDecorator<,>), LifeStyles.Singleton);

            //register our decorator that handles table creation errors
            container.RegisterDecorator(
                typeof(ICommandHandlerWithOutput<CreateJobTablesCommand<ITable>, QueueCreationResult>),
                typeof(CreateJobTablesCommandDecorator), LifeStyles.Singleton);

            //register our decorator that handles table creation errors
            container.RegisterDecorator(
                typeof(ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<ITable>, QueueCreationResult>),
                typeof(CreateQueueTablesAndSaveConfigurationDecorator), LifeStyles.Singleton);
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
        }

        /// <summary>
        /// Allows the transport to set default configuration settings or other values
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="registrationType">Type of the registration.</param>
        /// <param name="connectionType">Type of the connection.</param>
        public override void SetDefaultsIfNeeded(IContainer container, RegistrationTypes registrationType, ConnectionTypes connectionType)
        {
            var factory = container.GetInstance<IPostgreSqlMessageQueueTransportOptionsFactory>();
            var options = factory.Create();
            var configurationSend = container.GetInstance<QueueProducerConfiguration>();
            var configurationReceive = container.GetInstance<QueueConsumerConfiguration>();

            configurationSend.AdditionalConfiguration.SetSetting("PostgreSQLMessageQueueTransportOptions", options);
            configurationReceive.AdditionalConfiguration.SetSetting("PostgreSQLMessageQueueTransportOptions", options);

            var transportReceive = container.GetInstance<TransportConfigurationReceive>();


            transportReceive.HeartBeatSupported = options.EnableHeartBeat && options.EnableStatus &&
                                                  !options.EnableHoldTransactionUntilMessageCommited;

            transportReceive.MessageExpirationSupported = options.EnableMessageExpiration ||
                                                          options.QueueType == QueueTypes.RpcReceive ||
                                                          options.QueueType == QueueTypes.RpcSend;

            transportReceive.MessageRollbackSupported = options.EnableStatus ||
                                                        options.EnableHoldTransactionUntilMessageCommited;

            transportReceive.QueueDelayBehavior.Clear();
            transportReceive.QueueDelayBehavior.Add(DefaultQueueDelay());
            transportReceive.FatalExceptionDelayBehavior.Clear();
            transportReceive.FatalExceptionDelayBehavior.Add(ExceptionDelay());

            transportReceive.LockFeatures();

            SetupHeartBeat(container);
            SetupMessageExpiration(container);
        }

        /// <summary>
        /// Gets the default fatal exception delay time spans
        /// </summary>
        /// <returns></returns>
        private IEnumerable<TimeSpan> ExceptionDelay()
        {
            var rc = new List<TimeSpan>(10)
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(8),
                TimeSpan.FromSeconds(13),
                TimeSpan.FromSeconds(21),
                TimeSpan.FromSeconds(34),
                TimeSpan.FromSeconds(55)
            };
            return rc;
        }

        /// <summary>
        /// Gets the default queue delay time spans
        /// </summary>
        /// <returns></returns>
        private IEnumerable<TimeSpan> DefaultQueueDelay()
        {
            var rc = new List<TimeSpan>(21)
            {
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(2000)
            };
            return rc;
        }

        /// <summary>
        /// Setup the heart beat.
        /// </summary>
        /// <param name="container">The container.</param>
        private void SetupHeartBeat(IContainer container)
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
        private void SetupMessageExpiration(IContainer container)
        {
            var configuration = container.GetInstance<IMessageExpirationConfiguration>();
            if (!configuration.Supported) return;
            configuration.MonitorTime = TimeSpan.FromMinutes(3);
            configuration.Enabled = true;
        }
    }
}
