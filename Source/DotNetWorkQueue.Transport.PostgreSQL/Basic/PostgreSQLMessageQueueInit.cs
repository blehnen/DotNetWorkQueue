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
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Message;
using DotNetWorkQueue.Validation;
using Npgsql;
using Polly;
using FindExpiredRecordsToDeleteQueryPrepareHandler = DotNetWorkQueue.Transport.PostgreSQL.Basic.QueryPrepareHandler.FindExpiredRecordsToDeleteQueryPrepareHandler;
using FindErrorRecordsToDeleteQueryPrepareHandler = DotNetWorkQueue.Transport.PostgreSQL.Basic.QueryPrepareHandler.FindErrorRecordsToDeleteQueryPrepareHandler;
using FindRecordsToResetByHeartBeatQueryPrepareHandler = DotNetWorkQueue.Transport.PostgreSQL.Basic.QueryPrepareHandler.FindRecordsToResetByHeartBeatQueryPrepareHandler;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <inheritdoc />
    public class PostgreSqlMessageQueueInit : TransportMessageQueueSharedInit
    {
        /// <inheritdoc />
        public override void RegisterImplementations(IContainer container, RegistrationTypes registrationType,
            string connection, string queue)
        {
            Guard.NotNull(() => container, container);
            base.RegisterImplementations(container, registrationType, connection, queue);

            var init = new RelationalDatabaseMessageQueueInit();
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
            container.Register<IGetPreviousMessageErrors, GetPreviousMessageErrors>(LifeStyles.Singleton);

            container.Register<IRemoveMessage, RemoveMessage>(LifeStyles.Singleton);

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
            container.Register<ITransportRollbackMessage, RollbackMessage>(LifeStyles.Singleton);
            container.Register<ReceiveMessage>(LifeStyles.Transient);
            container.Register<IBuildMoveToErrorQueueSql, BuildMoveToErrorQueueSql>(LifeStyles.Singleton);
            //**receive

            //reset heart beat 
            container
                .Register<IPrepareCommandHandler<ResetHeartBeatCommand>,
                    ResetHeartBeatCommandPrepareHandler>(LifeStyles.Singleton);

            //delete table - need lower case
            container
                .Register<IPrepareCommandHandler<DeleteTableCommand>,
                    DeleteTableCommandPrepareHandler>(LifeStyles.Singleton);

            //explicit registration of our job exists query
            container
                .Register<RelationalDatabase.IQueryHandler<DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>,
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

            //error messages
            container
                .Register<IPrepareQueryHandler<FindErrorMessagesToDeleteQuery, IEnumerable<long>>,
                    FindErrorRecordsToDeleteQueryPrepareHandler>(LifeStyles.Singleton);

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
                .Register<RelationalDatabase.IQueryHandler<GetQueueOptionsQuery<PostgreSqlMessageQueueTransportOptions>,
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

            container.RegisterDecorator(typeof(RelationalDatabase.IQueryHandler<,>),
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

            //trace fallback command
            container.RegisterDecorator(
                typeof(ICommandHandler<RollbackMessageCommand>),
                typeof(DotNetWorkQueue.Transport.PostgreSQL.Trace.Decorator.RollbackMessageCommandHandlerDecorator), LifeStyles.Singleton);

            //trace sending a message so that we can add specific tags
            container.RegisterDecorator(
                typeof(ICommandHandlerWithOutput<SendMessageCommand, long>),
                typeof(DotNetWorkQueue.Transport.PostgreSQL.Trace.Decorator.SendMessageCommandHandlerDecorator), LifeStyles.Singleton);

            container.RegisterDecorator(
                typeof(ICommandHandlerWithOutputAsync<SendMessageCommand, long>),
                typeof(DotNetWorkQueue.Transport.PostgreSQL.Trace.Decorator.SendMessageCommandHandlerAsyncDecorator), LifeStyles.Singleton);
        }
        /// <inheritdoc />
        public override void SetDefaultsIfNeeded(IContainer container, RegistrationTypes registrationType, ConnectionTypes connectionType)
        {
            var init = new RelationalDatabaseMessageQueueInit();
            init.SetDefaultsIfNeeded(container, "PostgreSQLMessageQueueTransportOptions", "PostgreSQLMessageQueueTransportOptions");
            SetupPolicy(container);
        }

        private void SetupPolicy(IContainer container)
        {
            RetrySqlPolicyCreation.Register(container);
        }
    }
}
