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
using System.Linq;
using System.Reflection;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Policies;
using DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandPrepareHandler;
using DotNetWorkQueue.Transport.PostgreSQL.Basic.Factory;
using DotNetWorkQueue.Transport.PostgreSQL.Basic.Message;
using DotNetWorkQueue.Transport.PostgreSQL.Basic.Time;
using DotNetWorkQueue.Transport.PostgreSQL.Decorator;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler;
using DotNetWorkQueue.Validation;
using Npgsql;
using Polly;
using FindExpiredRecordsToDeleteQueryPrepareHandler = DotNetWorkQueue.Transport.PostgreSQL.Basic.QueryPrepareHandler.FindExpiredRecordsToDeleteQueryPrepareHandler;
using FindRecordsToResetByHeartBeatQueryPrepareHandler = DotNetWorkQueue.Transport.PostgreSQL.Basic.QueryPrepareHandler.FindRecordsToResetByHeartBeatQueryPrepareHandler;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <inheritdoc />
    public class PostgreSqlMessageQueueInit : TransportInitDuplex
    {
        /// <inheritdoc />
        public override void RegisterImplementations(IContainer container, RegistrationTypes registrationType,
            string connection, string queue)
        {
            Guard.NotNull(() => container, container);

            var init = new MessageQueueInit();
            init.RegisterStandardImplementations(container, Assembly.GetAssembly(GetType()));

            //**all
            container.Register<IDbConnectionFactory, DbConnectionFactory>(LifeStyles.Singleton);
            container.Register<PostgreSqlMessageQueueSchema>(LifeStyles.Singleton);
            container.Register<IQueueCreation, PostgreSqlMessageQueueCreation>(LifeStyles.Singleton);
            container.Register<IJobSchedulerLastKnownEvent, PostgreSqlJobSchedulerLastKnownEvent>(LifeStyles.Singleton);
            container.Register<IOptionsSerialization, OptionsSerialization>(LifeStyles.Singleton);
            container.Register<PostgreSqlJobSchema>(LifeStyles.Singleton);
            container.Register<ISendJobToQueue, PostgreSqlSendJobToQueue>(LifeStyles.Singleton);
            container.Register<CommandStringCache, PostgreSqlCommandStringCache>(LifeStyles.Singleton);
            container.Register<IJobSchema, PostgreSqlJobSchema>(LifeStyles.Singleton);
            container.Register<IReadColumn, ReadColumn>(LifeStyles.Singleton);
            container.Register<ITransportOptionsFactory, TransportOptionsFactory>(LifeStyles.Singleton);

            container.Register<IGetTime, PostgreSqlTime>(LifeStyles.Singleton);
            container.Register<IGetFirstMessageDeliveryTime, GetFirstMessageDeliveryTime>(LifeStyles.Singleton);
            container
                .Register
                <IPostgreSqlMessageQueueTransportOptionsFactory, PostgreSqlMessageQueueTransportOptionsFactory>(
                    LifeStyles.Singleton);

            container.Register<PostgreSqlCommandStringCache>(LifeStyles.Singleton);
            container.Register<IConnectionInformation>(() => new SqlConnectionInformation(queue, connection),
                LifeStyles.Singleton);

            container.Register<PostgreSqlMessageQueueTransportOptions>(LifeStyles.Singleton);
            container.Register<IConnectionHeader<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand>, ConnectionHeader<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand>>(LifeStyles.Singleton);
            //**all

            //**receive
            container.Register<IReceiveMessages, PostgreSqlMessageQueueReceive>(LifeStyles.Transient);
            container.Register<IConnectionHolderFactory<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand>, ConnectionHolderFactory>(LifeStyles.Singleton);
            container.Register<CommitMessage>(LifeStyles.Transient);
            container.Register<RollbackMessage>(LifeStyles.Transient);
            container.Register<HandleMessage>(LifeStyles.Transient);
            container.Register<ReceiveMessage>(LifeStyles.Transient);
            container.Register<IBuildMoveToErrorQueueSql, BuildMoveToErrorQueueSql>(LifeStyles.Singleton);
            //**receive

            //reset heart beat 
            container
                .Register<IPrepareCommandHandler<ResetHeartBeatCommand>,
                    ResetHeartBeatCommandPrepareHandler>(LifeStyles.Singleton);

            //explicit registration of our job exists query
            container
                .Register<IQueryHandler<DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>,
                        QueueStatuses>,
                    DoesJobExistQueryHandler<NpgsqlConnection, NpgsqlTransaction>>(LifeStyles.Singleton);

            //because we have an explicit registration for job exists, we need to explicitly register the prepare statement
            container
                .Register<IPrepareQueryHandler<DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>,
                        QueueStatuses>,
                    DoesJobExistQueryPrepareHandler<NpgsqlConnection, NpgsqlTransaction>>(LifeStyles.Singleton);

            //expired messages
            container
                .Register<IPrepareQueryHandler<FindExpiredMessagesToDeleteQuery, IEnumerable<long>>,
                    FindExpiredRecordsToDeleteQueryPrepareHandler>(LifeStyles.Singleton);

            //heartbeat
            container
                .Register<IPrepareQueryHandler<FindMessagesToResetByHeartBeatQuery, IEnumerable<MessageToReset>>,
                    FindRecordsToResetByHeartBeatQueryPrepareHandler>(LifeStyles.Singleton);

            container
                .Register<ICommandHandlerWithOutput<DeleteTransactionalMessageCommand, long>,
                    DeleteTransactionalMessageCommandHandler<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand>>(LifeStyles.Singleton);

            container
                .Register<ICommandHandler<MoveRecordToErrorQueueCommand>,
                    MoveRecordToErrorQueueCommandHandler<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand>>(LifeStyles.Singleton);

            //explicit registration of options
            container
                .Register<IQueryHandler<GetQueueOptionsQuery<PostgreSqlMessageQueueTransportOptions>,
                        PostgreSqlMessageQueueTransportOptions>,
                    GetQueueOptionsQueryHandler<PostgreSqlMessageQueueTransportOptions>>(LifeStyles.Singleton);

            container
                .Register<IPrepareQueryHandler<GetQueueOptionsQuery<PostgreSqlMessageQueueTransportOptions>,
                        PostgreSqlMessageQueueTransportOptions>,
                    GetQueueOptionsQueryPrepareHandler<PostgreSqlMessageQueueTransportOptions>>(LifeStyles.Singleton);

            container.RegisterDecorator(typeof(IPrepareQueryHandler<GetColumnNamesFromTableQuery, List<string>>),
                typeof(GetColumnNamesFromTableQueryPrepareDecorator), LifeStyles.Singleton);

            container.RegisterDecorator(typeof(IPrepareQueryHandler<GetTableExistsQuery, bool>),
                typeof(GetTableExistsQueryPrepareDecorator), LifeStyles.Singleton);

            container.RegisterDecorator(typeof(IPrepareQueryHandler<GetTableExistsTransactionQuery, bool>),
                typeof(GetTableExistsTransactionQueryPrepareDecorator), LifeStyles.Singleton);

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
                typeof(ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<ITable>,
                    QueueCreationResult>),
                typeof(CreateQueueTablesAndSaveConfigurationDecorator), LifeStyles.Singleton);
        }
        /// <inheritdoc />
        public override void SetDefaultsIfNeeded(IContainer container, RegistrationTypes registrationType, ConnectionTypes connectionType)
        {
            var init = new MessageQueueInit();
            init.SetDefaultsIfNeeded(container, "PostgreSQLMessageQueueTransportOptions", "PostgreSQLMessageQueueTransportOptions");
            SetupPolicy(container);
        }

        private void SetupPolicy(IContainer container)
        {
            var policies = container.GetInstance<IPolicies>();
            var log = container.GetInstance<ILogFactory>().Create();

            var retrySql = Policy
                .Handle<PostgresException>(ex => RetryablePostGreErrors.Errors.Contains(ex.SqlState))
                .WaitAndRetry(
                    RetryConstants.RetryCount,
                    retryAttempt => TimeSpan.FromSeconds(ThreadSafeRandom.Next(RetryConstants.MinWait, RetryConstants.MaxWait)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        log.WarnException($"An error has occurred; we will try to re-run the transaction in {timeSpan.TotalMilliseconds} ms. An error has occured {retryCount} times", exception);
                    });


            //RetryCommandHandler
            policies.TransportDefinition.TryAdd(TransportPolicyDefinitions.RetryCommandHandler,
                new TransportPolicyDefinition(
                    TransportPolicyDefinitions.RetryCommandHandler,
                    "A policy for retrying a failed command. This checks specific" +
                    "PostGres server errors, such as deadlocks, and retries the command" +
                    "after a short pause"));
            policies.Registry[TransportPolicyDefinitions.RetryCommandHandler] = retrySql;

            //RetryQueryHandler
            policies.TransportDefinition.TryAdd(TransportPolicyDefinitions.RetryQueryHandler,
                new TransportPolicyDefinition(
                    TransportPolicyDefinitions.RetryQueryHandler,
                    "A policy for retrying a failed query. This checks specific" +
                    "PostGres server errors, such as deadlocks, and retries the query" +
                    "after a short pause"));
            policies.Registry[TransportPolicyDefinitions.RetryQueryHandler] = retrySql;
        }
    }
}
