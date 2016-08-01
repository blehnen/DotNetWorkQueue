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
using System.Collections.Generic;
using System.Reflection;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Transport.SQLite.Basic.Factory;
using DotNetWorkQueue.Transport.SQLite.Basic.Message;
using CommitMessage = DotNetWorkQueue.Transport.SQLite.Basic.Message.CommitMessage;
using DotNetWorkQueue.Transport.SQLite.Basic.QueryHandler;
using DotNetWorkQueue.Transport.SQLite.Decorator;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// Registers the implementations for the SQLite queue into the IoC container.
    /// </summary>
    public class SqLiteMessageQueueInit : TransportInitDuplex
    {
        /// <summary>
        /// Registers the implementations.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="registrationType">Type of the registration.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="queue">The queue.</param>
        public override void RegisterImplementations(IContainer container, RegistrationTypes registrationType, string connection, string queue)
        {
            Guard.NotNull(() => container, container);

            //**all
            container.RegisterNonScopedSingleton<ICreationScope>(new CreationScope());

            container.Register<SqLiteMessageQueueSchema>(LifeStyles.Singleton);
            container.Register<IQueueCreation, SqLiteMessageQueueCreation>(LifeStyles.Singleton);
            container.Register<SqLiteMessageQueueStatusQueries>(LifeStyles.Singleton);
            container.Register<IQueueStatusProvider, SqLiteQueueStatusProvider>(LifeStyles.Singleton);
            container.Register<IJobSchedulerLastKnownEvent, SqliteJobSchedulerLastKnownEvent>(LifeStyles.Singleton);
            container.Register<ISendJobToQueue, SqliteSendToJobQueue>(LifeStyles.Singleton);
            container.Register<IJobTableCreation, SqliteJobTableCreation>(LifeStyles.Singleton);
            container.Register<SqliteJobSchema>(LifeStyles.Singleton);
            container.Register<CreateJobMetaData>(LifeStyles.Singleton);

            container.Register<IGetFirstMessageDeliveryTime, GetFirstMessageDeliveryTime>(LifeStyles.Singleton);
            container
                .Register
                <ISqLiteMessageQueueTransportOptionsFactory, SqLiteMessageQueueTransportOptionsFactory>(
                    LifeStyles.Singleton);

            container.Register<SqLiteCommandStringCache>(LifeStyles.Singleton);

            container.Register<IConnectionInformation>(() => new SqliteConnectionInformation(queue, connection), LifeStyles.Singleton);
            container.Register<ICorrelationIdFactory, SqLiteMessageQueueCorrelationIdFactory>(
                LifeStyles.Singleton);

            container.Register<BuildDequeueCommand>(LifeStyles.Singleton);
            container.Register<MessageDeQueue>(LifeStyles.Singleton);

            container.Register<SqLiteMessageQueueTransportOptions>(LifeStyles.Singleton);

            container.Register<TableNameHelper>(LifeStyles.Singleton);
            container.Register<SqlHeaders>(LifeStyles.Singleton);
            container.Register<IClearExpiredMessages, SqLiteMessageQueueClearExpiredMessages>(LifeStyles.Singleton);

            container.Register<ISqLiteTransactionFactory, SqLiteTransactionFactory>(LifeStyles.Singleton);
            container.Register<ISqLiteTransactionWrapper, SqLiteTransactionWrapper>(LifeStyles.Transient);
            //**all

            //**send
            container.Register<ISendMessages, SqLiteMessageQueueSend>(LifeStyles.Singleton);
            //**send


            //**receive
            container.Register<IReceiveMessages, SqLiteMessageQueueReceive>(LifeStyles.Transient);
            container.Register<CommitMessage>(LifeStyles.Transient);
            container.Register<RollbackMessage>(LifeStyles.Transient);
            container.Register<HandleMessage>(LifeStyles.Transient);
            container.Register<Message.ReceiveMessage>(LifeStyles.Transient);

            container.Register<IResetHeartBeat, SqLiteMessageQueueResetHeartBeat>(LifeStyles.Singleton);
            container.Register<ISendHeartBeat, SqLiteMessageQueueSendHeartBeat>(LifeStyles.Singleton);
            container.Register<IReceiveMessagesFactory, ReceiveMessagesFactory>(LifeStyles.Singleton);
            container.Register<IReceivePoisonMessage, SqLiteQueueReceivePoisonMessage>(LifeStyles.Singleton);
            container.Register<IReceiveMessagesError, SqLiteMessageQueueReceiveErrorMessage>(LifeStyles.Singleton);
            //**receive

            var target = Assembly.GetAssembly(GetType());

            //commands and decorators
            // Go look in all assemblies and register all implementations
            // of ICommandHandlerWithOutput<T> by their closed interface:
            container.Register(typeof (ICommandHandlerWithOutput<,>), LifeStyles.Singleton,
                target);

            //commands and decorators
            // Go look in all assemblies and register all implementations
            // of ICommandHandlerWithOutputAsync<T> by their closed interface:
            container.Register(typeof(ICommandHandlerWithOutputAsync<,>), LifeStyles.Singleton,
                target);

            // Go look in all assemblies and register all implementations
            // of ICommandHandler<T> by their closed interface:
            container.Register(typeof (ICommandHandler<>), LifeStyles.Singleton,
                target);

            // Go look in all assemblies and register all implementations
            // of IQueryHandler<T> by their closed interface:
            container.Register(typeof (IQueryHandler<,>), LifeStyles.Singleton,
                target);

            container.RegisterDecorator(typeof(ISqLiteTransactionWrapper),
               typeof(BeginTransactionRetryDecorator), LifeStyles.Transient);
        }

        /// <summary>
        /// Allows the transport to set default configuration settings or other values
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="registrationType">Type of the registration.</param>
        /// <param name="connectionType">Type of the connection.</param>
        public override void SetDefaultsIfNeeded(IContainer container, RegistrationTypes registrationType, ConnectionTypes connectionType)
        {
            var factory = container.GetInstance<ISqLiteMessageQueueTransportOptionsFactory>();
            var options = factory.Create();
            var configurationSend = container.GetInstance<QueueProducerConfiguration>();
            var configurationReceive = container.GetInstance<QueueConsumerConfiguration>();

            configurationSend.AdditionalConfiguration.SetSetting("SQLiteMessageQueueTransportOptions", options);
            configurationReceive.AdditionalConfiguration.SetSetting("SQLiteMessageQueueTransportOptions", options);

            var transportReceive = container.GetInstance<TransportConfigurationReceive>();


            transportReceive.HeartBeatSupported = options.EnableHeartBeat && options.EnableStatus;

            transportReceive.MessageExpirationSupported = options.EnableMessageExpiration ||
                                                          options.QueueType == QueueTypes.RpcReceive ||
                                                          options.QueueType == QueueTypes.RpcSend;

            transportReceive.MessageRollbackSupported = options.EnableStatus;

            transportReceive.QueueDelayBehavior.Clear();
            transportReceive.QueueDelayBehavior.Add(DefaultQueueDelay());
            transportReceive.FatalExceptionDelayBehavior.Clear();
            transportReceive.FatalExceptionDelayBehavior.Add(ExceptionDelay());

            transportReceive.LockFeatures();

            SetupHeartBeat(container);
            SetupMessageExpiration(container);

            //create in memory hold
            var connection = container.GetInstance<IConnectionInformation>();
            var fileName = GetFileNameFromConnectionString.GetFileName(connection.ConnectionString);
            if(fileName.IsInMemory)
            {
                var scope = container.GetInstance<ICreationScope>();
                var holder = new SqLiteHoldConnection();
                holder.AddConnectionIfNeeded(connection);
                scope.AddScopedObject(holder);
            }
        }

        /// <summary>
        /// Gets the default fatal exception delay timespans
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
        /// Gets the default queue delay timespans
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
