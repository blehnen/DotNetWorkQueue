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
using System.Data.SQLite;
using System.Reflection;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Factory;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler;
using DotNetWorkQueue.Transport.SQLite.Basic.CommandPrepareHandler;
using DotNetWorkQueue.Transport.SQLite.Basic.Factory;
using DotNetWorkQueue.Transport.SQLite.Basic.Message;
using CommitMessage = DotNetWorkQueue.Transport.SQLite.Basic.Message.CommitMessage;
using DotNetWorkQueue.Transport.SQLite.Basic.QueryHandler;
using DotNetWorkQueue.Transport.SQLite.Decorator;
using DotNetWorkQueue.Validation;

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
        public override void RegisterImplementations(IContainer container, RegistrationTypes registrationType,
            string connection, string queue)
        {
            Guard.NotNull(() => container, container);

            //**all
            container.RegisterNonScopedSingleton<ICreationScope>(new CreationScope());

            container.Register<SqLiteMessageQueueSchema>(LifeStyles.Singleton);
            container.Register<IQueueCreation, SqLiteMessageQueueCreation>(LifeStyles.Singleton);
            container.Register<QueueStatusQueries>(LifeStyles.Singleton);
            container.Register<IQueueStatusProvider, QueueStatusProvider>(LifeStyles.Singleton);
            container.Register<IJobSchedulerLastKnownEvent, SqliteJobSchedulerLastKnownEvent>(LifeStyles.Singleton);
            container.Register<ISendJobToQueue, SqliteSendToJobQueue>(LifeStyles.Singleton);
            container.Register<IJobTableCreation, JobTableCreation>(LifeStyles.Singleton);
            container.Register<IDbConnectionFactory, DbConnectionFactory>(LifeStyles.Singleton);
            container.Register<SqliteJobSchema>(LifeStyles.Singleton);
            container.Register<CreateJobMetaData>(LifeStyles.Singleton);
            container.Register<IOptionsSerialization, OptionsSerialization>(LifeStyles.Singleton);
            container.Register<CommandStringCache, SqLiteCommandStringCache>(LifeStyles.Singleton);
            container.Register<ITransportOptionsFactory, TransportOptionsFactory>(LifeStyles.Singleton);
            container.Register<ITransactionFactory, TransactionFactory>(LifeStyles.Singleton);
            container.Register<IJobSchema, SqliteJobSchema>(LifeStyles.Singleton);
            container.Register<IDateTimeOffsetParser, DateTimeOffsetParser>(LifeStyles.Singleton);
            container.Register<IReadColumn, ReadColumn>(LifeStyles.Singleton);

            container.Register<IGetFirstMessageDeliveryTime, GetFirstMessageDeliveryTime>(LifeStyles.Singleton);
            container
                .Register
                <ISqLiteMessageQueueTransportOptionsFactory, SqLiteMessageQueueTransportOptionsFactory>(
                    LifeStyles.Singleton);

            container.Register<SqLiteCommandStringCache>(LifeStyles.Singleton);

            container.Register<IConnectionInformation>(() => new SqliteConnectionInformation(queue, connection),
                LifeStyles.Singleton);
            container.Register<ICorrelationIdFactory, CorrelationIdFactory>(
                LifeStyles.Singleton);

            container.Register<BuildDequeueCommand>(LifeStyles.Singleton);
            container.Register<MessageDeQueue>(LifeStyles.Singleton);

            container.Register<SqLiteMessageQueueTransportOptions>(LifeStyles.Singleton);

            container.Register<TableNameHelper>(LifeStyles.Singleton);
            container.Register<SqlHeaders>(LifeStyles.Singleton);
            container.Register<IClearExpiredMessages, ClearExpiredMessages>(LifeStyles.Singleton);

            container.Register<ISqLiteTransactionFactory, SqLiteTransactionFactory>(LifeStyles.Singleton);
            container.Register<ISqLiteTransactionWrapper, SqLiteTransactionWrapper>(LifeStyles.Transient);
            //**all

            //**send
            container.Register<ISendMessages, SendMessages>(LifeStyles.Singleton);
            //**send


            //**receive
            container.Register<IReceiveMessages, SqLiteMessageQueueReceive>(LifeStyles.Transient);
            container.Register<CommitMessage>(LifeStyles.Transient);
            container.Register<RollbackMessage>(LifeStyles.Transient);
            container.Register<HandleMessage>(LifeStyles.Transient);
            container.Register<Message.ReceiveMessage>(LifeStyles.Transient);

            container.Register<IResetHeartBeat, ResetHeartBeat>(LifeStyles.Singleton);
            container.Register<ISendHeartBeat, SendHeartBeat>(LifeStyles.Singleton);
            container.Register<IReceiveMessagesFactory, ReceiveMessagesFactory>(LifeStyles.Singleton);
            container.Register<IReceivePoisonMessage, ReceivePoisonMessage>(LifeStyles.Singleton);
            container.Register<IReceiveMessagesError, ReceiveErrorMessage>(LifeStyles.Singleton);
            container.Register<IIncreaseQueueDelay, SqlHeaders>(LifeStyles.Singleton);
            //**receive

            var target = Assembly.GetAssembly(GetType());
            RegisterCommands(container, target);

            var target2 = Assembly.GetAssembly(typeof(ITable));
            if (target.FullName != target2.FullName)
                RegisterCommands(container, target2);

            //reset heart beat 
                container
                    .Register<IPrepareCommandHandler<ResetHeartBeatCommand>,
                        ResetHeartBeatCommandPrepareHandler>(LifeStyles.Singleton);

            //explicit registration of our job exists query
            container
                .Register<IQueryHandler<DoesJobExistQuery<SQLiteConnection, SQLiteTransaction>,
                        QueueStatuses>,
                    DoesJobExistQueryHandler<SQLiteConnection, SQLiteTransaction>>(LifeStyles.Singleton);

            //because we have an explicit registration for job exists, we need to explicitly register the prepare statement
            container
                .Register<IPrepareQueryHandler<DoesJobExistQuery<SQLiteConnection, SQLiteTransaction>,
                        QueueStatuses>,
                    DoesJobExistQueryPrepareHandler<SQLiteConnection, SQLiteTransaction>>(LifeStyles.Singleton);

            //expired messages
            container
                .Register<IPrepareQueryHandler<FindExpiredMessagesToDeleteQuery, IEnumerable<long>>,
                    QueryPrepareHandler.FindExpiredRecordsToDeleteQueryPrepareHandler>(LifeStyles.Singleton);

            //heartbeat
            container
                .Register<IPrepareQueryHandler<FindMessagesToResetByHeartBeatQuery, IEnumerable<MessageToReset>>,
                    QueryPrepareHandler.FindRecordsToResetByHeartBeatQueryPrepareHandler>(LifeStyles.Singleton);

            //explicit registration of options
            container
                .Register<IQueryHandler<GetQueueOptionsQuery<SqLiteMessageQueueTransportOptions>, SqLiteMessageQueueTransportOptions>,
                    GetQueueOptionsQueryHandler<SqLiteMessageQueueTransportOptions>>(LifeStyles.Singleton);

            container
                .Register<IPrepareQueryHandler<GetQueueOptionsQuery<SqLiteMessageQueueTransportOptions>,
                        SqLiteMessageQueueTransportOptions>,
                    GetQueueOptionsQueryPrepareHandler<SqLiteMessageQueueTransportOptions>>(LifeStyles.Singleton);

            container.RegisterDecorator(typeof(ISqLiteTransactionWrapper),
                typeof(BeginTransactionRetryDecorator), LifeStyles.Transient);

            //register our decorator that handles table creation errors
            container.RegisterDecorator(
                typeof(ICommandHandlerWithOutput<CreateJobTablesCommand<ITable>, QueueCreationResult>),
                typeof(CreateJobTablesCommandDecorator), LifeStyles.Singleton);

            //register our decorator that handles table creation errors
            container.RegisterDecorator(
                typeof(ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<ITable>,
                    QueueCreationResult>),
                typeof(CreateQueueTablesAndSaveConfigurationDecorator), LifeStyles.Singleton);

            //register our decorator for deleting messages
            container.RegisterDecorator(
                typeof(ICommandHandlerWithOutput<DeleteMessageCommand, long>),
                typeof(DeleteMessageCommandDecorator), LifeStyles.Singleton);

            //register our decorator for setting the status
            container.RegisterDecorator(
                typeof(ICommandHandler<DeleteStatusTableStatusCommand>),
                typeof(SetStatusTableStatusCommandDecorator), LifeStyles.Singleton);

            //register our decorator for resetting the heart beat
            container.RegisterDecorator(
                typeof(ICommandHandlerWithOutput<ResetHeartBeatCommand, long>),
                typeof(ResetHeartBeatCommandDecorator), LifeStyles.Singleton);

            container.RegisterDecorator(
                typeof(IQueryHandler<GetErrorRecordExistsQuery, bool>),
                typeof(GetErrorRecordExistsQueryDecorator), LifeStyles.Singleton);

            container.RegisterDecorator(
                typeof(IQueryHandler<FindExpiredMessagesToDeleteQuery, IEnumerable<long>>),
                typeof(FindExpiredRecordsToDeleteDecorator), LifeStyles.Singleton);

            container.RegisterDecorator(
                typeof(IQueryHandler<FindMessagesToResetByHeartBeatQuery, IEnumerable<MessageToReset>>),
                typeof(FindRecordsToResetByHeartBeatDecorator), LifeStyles.Singleton);

            container.RegisterDecorator(
                typeof(IQueryHandler<GetColumnNamesFromTableQuery, List<string>>),
                typeof(GetColumnNamesFromTableDecorator), LifeStyles.Singleton);

            container.RegisterDecorator(
                typeof(IQueryHandler<GetTableExistsQuery, bool>),
                typeof(GetTableExistsDecorator), LifeStyles.Singleton);

            container.RegisterDecorator(
                typeof(IQueryHandler<DoesJobExistQuery<SQLiteConnection, SQLiteTransaction>, QueueStatuses>),
                typeof(DoesJobExistDecorator), LifeStyles.Singleton);

            container.RegisterDecorator(
                typeof(ICommandHandlerWithOutput<DeleteQueueTablesCommand, QueueRemoveResult>),
                typeof(DeleteQueueTablesDecorator), LifeStyles.Singleton);

            container.RegisterDecorator(
                typeof(ICommandHandler<SetErrorCountCommand>),
                typeof(SetErrorCountCommandDecorator), LifeStyles.Singleton);
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
            container.Register(typeof(IPrepareQueryHandler<,>), LifeStyles.Singleton,
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
            transportReceive.QueueDelayBehavior.Add(DefaultQueueDelay.GetDefaultQueueDelay());
            transportReceive.FatalExceptionDelayBehavior.Clear();
            transportReceive.FatalExceptionDelayBehavior.Add(ExceptionDelay.GetExceptionDelay());

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
