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
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Policies;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler;
using DotNetWorkQueue.Transport.SQLite.Basic.CommandPrepareHandler;
using DotNetWorkQueue.Transport.SQLite.Basic.Factory;
using DotNetWorkQueue.Transport.SQLite.Basic.Message;
using DotNetWorkQueue.Transport.SQLite.Basic.QueryHandler;
using DotNetWorkQueue.Transport.SQLite.Decorator;
using DotNetWorkQueue.Validation;
using Polly;
using FindExpiredRecordsToDeleteQueryPrepareHandler = DotNetWorkQueue.Transport.SQLite.Basic.QueryPrepareHandler.FindExpiredRecordsToDeleteQueryPrepareHandler;
using FindRecordsToResetByHeartBeatQueryPrepareHandler = DotNetWorkQueue.Transport.SQLite.Basic.QueryPrepareHandler.FindRecordsToResetByHeartBeatQueryPrepareHandler;
using ReceiveMessage = DotNetWorkQueue.Transport.SQLite.Basic.Message.ReceiveMessage;

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
            var init = new MessageQueueInit();
            init.RegisterStandardImplementations(container, Assembly.GetAssembly(GetType()));

            //**all
            container.RegisterNonScopedSingleton<ICreationScope>(new CreationScope());

            container.Register<SqLiteMessageQueueSchema>(LifeStyles.Singleton);
            container.Register<IQueueCreation, SqLiteMessageQueueCreation>(LifeStyles.Singleton);
            container.Register<IJobSchedulerLastKnownEvent, SqliteJobSchedulerLastKnownEvent>(LifeStyles.Singleton);
            container.Register<ISendJobToQueue, SqliteSendToJobQueue>(LifeStyles.Singleton);
            container.Register<IDbConnectionFactory, DbConnectionFactory>(LifeStyles.Singleton);
            container.Register<SqliteJobSchema>(LifeStyles.Singleton);
            container.Register<IOptionsSerialization, OptionsSerialization>(LifeStyles.Singleton);
            container.Register<CommandStringCache, SqLiteCommandStringCache>(LifeStyles.Singleton);
            container.Register<ITransportOptionsFactory, TransportOptionsFactory>(LifeStyles.Singleton);
            container.Register<ITransactionFactory, TransactionFactory>(LifeStyles.Singleton);
            container.Register<IJobSchema, SqliteJobSchema>(LifeStyles.Singleton);
            container.Register<IReadColumn, ReadColumn>(LifeStyles.Singleton);
            container.Register<IBuildMoveToErrorQueueSql, BuildMoveToErrorQueueSql>(LifeStyles.Singleton);

            container.Register<IGetFirstMessageDeliveryTime, GetFirstMessageDeliveryTime>(LifeStyles.Singleton);
            container
                .Register
                <ISqLiteMessageQueueTransportOptionsFactory, SqLiteMessageQueueTransportOptionsFactory>(
                    LifeStyles.Singleton);

            container.Register<SqLiteCommandStringCache>(LifeStyles.Singleton);

            container.Register<IConnectionInformation>(() => new SqliteConnectionInformation(queue, connection),
                LifeStyles.Singleton);
          
            container.Register<BuildDequeueCommand>(LifeStyles.Singleton);
            container.Register<MessageDeQueue>(LifeStyles.Singleton);

            container.Register<SqLiteMessageQueueTransportOptions>(LifeStyles.Singleton);

            container.Register<IConnectionHeader<SQLiteConnection, SQLiteTransaction, SQLiteCommand>, ConnectionHeader<SQLiteConnection, SQLiteTransaction, SQLiteCommand>>(LifeStyles.Singleton);

            container.Register<ISqLiteTransactionFactory, SqLiteTransactionFactory>(LifeStyles.Singleton);
            container.Register<ISqLiteTransactionWrapper, SqLiteTransactionWrapper>(LifeStyles.Transient);
            //**all

            //**receive
            container.Register<IReceiveMessages, SqLiteMessageQueueReceive>(LifeStyles.Transient);
            container.Register<CommitMessage>(LifeStyles.Transient);
            container.Register<RollbackMessage>(LifeStyles.Transient);
            container.Register<HandleMessage>(LifeStyles.Transient);
            container.Register<ReceiveMessage>(LifeStyles.Transient);
            //**receive

            //reset heart beat 
                container
                    .Register<IPrepareCommandHandler<ResetHeartBeatCommand>,
                        ResetHeartBeatCommandPrepareHandler>(LifeStyles.Singleton);

            container
                .Register<IPrepareCommandHandler<MoveRecordToErrorQueueCommand>,
                    MoveRecordToErrorQueueCommandPrepareHandler>(LifeStyles.Singleton);

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

            container
                .Register<ICommandHandler<MoveRecordToErrorQueueCommand>,
                    MoveRecordToErrorQueueCommandHandler<SQLiteConnection, SQLiteTransaction, SQLiteCommand>>(LifeStyles.Singleton);

            //expired messages
            container
                .Register<IPrepareQueryHandler<FindExpiredMessagesToDeleteQuery, IEnumerable<long>>,
                    FindExpiredRecordsToDeleteQueryPrepareHandler>(LifeStyles.Singleton);

            //heartbeat
            container
                .Register<IPrepareQueryHandler<FindMessagesToResetByHeartBeatQuery, IEnumerable<MessageToReset>>,
                    FindRecordsToResetByHeartBeatQueryPrepareHandler>(LifeStyles.Singleton);

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
                typeof(ICommandHandler<MoveRecordToErrorQueueCommand>),
                typeof(MoveRecordToErrorQueueCommandDecorator), LifeStyles.Singleton);

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

        /// <summary>
        /// Allows the transport to set default configuration settings or other values
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="registrationType">Type of the registration.</param>
        /// <param name="connectionType">Type of the connection.</param>
        public override void SetDefaultsIfNeeded(IContainer container, RegistrationTypes registrationType, ConnectionTypes connectionType)
        {
            var init = new MessageQueueInit();
            init.SetDefaultsIfNeeded(container, "SQLiteMessageQueueTransportOptions", "SQLiteMessageQueueTransportOptions");

            SetupPolicy(container);

            //create in memory hold
            var connection = container.GetInstance<IConnectionInformation>();
            var fileName = GetFileNameFromConnectionString.GetFileName(connection.ConnectionString);
            if (!fileName.IsInMemory) return;
            var scope = container.GetInstance<ICreationScope>();
            var holder = new SqLiteHoldConnection();
            holder.AddConnectionIfNeeded(connection);
            scope.AddScopedObject(holder);
        }

        private void SetupPolicy(IContainer container)
        {
            var policies = container.GetInstance<IPolicies>();
            var log = container.GetInstance<ILogFactory>().Create();

            var retrySql = Policy
                .Handle<SQLiteException>(ex => Enum.IsDefined(typeof(RetryableSqlErrors), ex.ErrorCode))
                .WaitAndRetry(
                    RetryConstants.RetryCount,
                    retryAttempt => TimeSpan.FromSeconds(ThreadSafeRandom.Next(RetryConstants.MinWait, RetryConstants.MaxWait)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        log.WarnException($"An error has occurred; we will try to re-run the transaction in {timeSpan.TotalMilliseconds} ms. An error has occured {retryCount} times", exception);
                    });


            //BeginTransaction
            policies.TransportDefinition.TryAdd(TransportPolicyDefinitions.BeginTransaction,
                new TransportPolicyDefinition(
                    TransportPolicyDefinitions.BeginTransaction,
                    "A policy for retrying a BeginTransaction command. Sqlite will fail to start" +
                    "a transaction if another one is in progress. This behavior lets us wait a little" +
                    "bit and try agian"));
            policies.Registry[TransportPolicyDefinitions.BeginTransaction] = retrySql;
        }
    }
}
