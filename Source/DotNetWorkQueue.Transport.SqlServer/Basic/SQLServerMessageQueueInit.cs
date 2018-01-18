using System;
using System.Data.SqlClient;
using System.Reflection;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Policies;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler;
using DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler;
using DotNetWorkQueue.Transport.SqlServer.Basic.Factory;
using DotNetWorkQueue.Transport.SqlServer.Basic.Message;
using DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler;
using DotNetWorkQueue.Transport.SqlServer.Basic.Time;
using DotNetWorkQueue.Transport.SqlServer.Decorator;
using DotNetWorkQueue.Validation;
using Polly;

namespace DotNetWorkQueue.Transport.SqlServer.Basic
{
    /// <summary>
    /// Registers the implementations for the SQL server queue into the IoC container.
    /// </summary>
    public class SqlServerMessageQueueInit : TransportInitDuplex
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

            var init = new RelationalDatabaseMessageQueueInit();
            init.RegisterStandardImplementations(container, Assembly.GetAssembly(GetType()));

            //**all
            container.Register<IDbConnectionFactory, DbConnectionFactory>(LifeStyles.Singleton);
            container.Register<SqlServerMessageQueueSchema>(LifeStyles.Singleton);
            container.Register<IQueueCreation, SqlServerMessageQueueCreation>(LifeStyles.Singleton);
            container.Register<IJobSchedulerLastKnownEvent, SqlServerJobSchedulerLastKnownEvent>(LifeStyles.Singleton);
            container.Register<SqlServerJobSchema>(LifeStyles.Singleton);
            container.Register<ISendJobToQueue, SqlServerSendJobToQueue>(LifeStyles.Singleton);
            container.Register<CommandStringCache, SqlServerCommandStringCache>(LifeStyles.Singleton);
            container.Register<IOptionsSerialization, OptionsSerialization>(LifeStyles.Singleton);
            container.Register<IJobSchema, SqlServerJobSchema>(LifeStyles.Singleton);
            container.Register<IReadColumn, ReadColumn>(LifeStyles.Singleton);
            container.Register<IBuildMoveToErrorQueueSql, BuildMoveToErrorQueueSql>(LifeStyles.Singleton);
            container.Register<ITransportOptionsFactory, TransportOptionsFactory>(LifeStyles.Singleton);

            container.Register<IGetTime, SqlServerTime>(LifeStyles.Singleton);
            container.Register<IGetFirstMessageDeliveryTime, GetFirstMessageDeliveryTime>(LifeStyles.Singleton);
            container
                .Register
                <ISqlServerMessageQueueTransportOptionsFactory, SqlServerMessageQueueTransportOptionsFactory>(
                    LifeStyles.Singleton);

            container.Register<ICreationScope, CreationScopeNoOp>(LifeStyles.Singleton);
            container.Register<SqlServerCommandStringCache>(LifeStyles.Singleton);

            container.Register<IConnectionInformation>(() => new SqlConnectionInformation(queue, connection), LifeStyles.Singleton);

            container.Register<SqlServerMessageQueueTransportOptions>(LifeStyles.Singleton);
            container.Register<IConnectionHeader<SqlConnection, SqlTransaction, SqlCommand>, ConnectionHeader<SqlConnection, SqlTransaction, SqlCommand>>(LifeStyles.Singleton);
            //**all

            //**receive
            container.Register<IReceiveMessages, SqlServerMessageQueueReceive>(LifeStyles.Transient);
            container.Register<IConnectionHolderFactory<SqlConnection, SqlTransaction, SqlCommand>, ConnectionHolderFactory>(LifeStyles.Singleton);
            container.Register<CommitMessage>(LifeStyles.Transient);
            container.Register<RollbackMessage>(LifeStyles.Transient);
            container.Register<HandleMessage>(LifeStyles.Transient);
            container.Register<ReceiveMessage>(LifeStyles.Transient);
            container.Register<CreateDequeueStatement>(LifeStyles.Singleton);
            container.Register<BuildDequeueCommand>(LifeStyles.Singleton);
            container.Register<ReadMessage>(LifeStyles.Singleton);
            //**receive

            //explicit registration of our job exists query
            container
                .Register<IQueryHandler<DoesJobExistQuery<SqlConnection, SqlTransaction>,
                        QueueStatuses>,
                    DoesJobExistQueryHandler<SqlConnection, SqlTransaction>>(LifeStyles.Singleton);

            //because we have an explicit registration for job exists, we need to explicitly register the prepare statement
            container
                .Register<IPrepareQueryHandler<DoesJobExistQuery<SqlConnection, SqlTransaction>,
                        QueueStatuses>,
                    DoesJobExistQueryPrepareHandler<SqlConnection, SqlTransaction>>(LifeStyles.Singleton);

            container
                .Register<ICommandHandlerWithOutput<SendHeartBeatCommand, DateTime?>,
                    SendHeartBeatCommandHandler>(LifeStyles.Singleton);

            container
                .Register<ICommandHandler<MoveRecordToErrorQueueCommand>,
                    MoveRecordToErrorQueueCommandHandler<SqlConnection, SqlTransaction, SqlCommand>>(LifeStyles.Singleton);

            //explicit registration of options
            container
                .Register<IQueryHandler<GetQueueOptionsQuery<SqlServerMessageQueueTransportOptions>, SqlServerMessageQueueTransportOptions>,
                    GetQueueOptionsQueryHandler<SqlServerMessageQueueTransportOptions>>(LifeStyles.Singleton);

            container
                .Register<ICommandHandlerWithOutput<DeleteTransactionalMessageCommand, long>,
                    DeleteTransactionalMessageCommandHandler<SqlConnection, SqlTransaction, SqlCommand>>(LifeStyles.Singleton);

            container
                .Register<IPrepareQueryHandler<GetQueueOptionsQuery<SqlServerMessageQueueTransportOptions>,
                        SqlServerMessageQueueTransportOptions>,
                    GetQueueOptionsQueryPrepareHandler<SqlServerMessageQueueTransportOptions>>(LifeStyles.Singleton);

            container.RegisterDecorator(typeof (ICommandHandlerWithOutput<,>),
                typeof (RetryCommandHandlerOutputDecorator<,>), LifeStyles.Singleton);

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

        /// <summary>
        /// Allows the transport to set default configuration settings or other values
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="registrationType">Type of the registration.</param>
        /// <param name="connectionType">Type of the connection.</param>
        public override void SetDefaultsIfNeeded(IContainer container, RegistrationTypes registrationType, ConnectionTypes connectionType)
        {
            var init = new RelationalDatabaseMessageQueueInit();
            init.SetDefaultsIfNeeded(container, "SqlServerMessageQueueTransportOptions", "SqlServerMessageQueueTransportOptions");

            SetupPolicy(container);
        }

        private void SetupPolicy(IContainer container)
        {
            var policies = container.GetInstance<IPolicies>();
            var log = container.GetInstance<ILogFactory>().Create();

            var retrySql = Policy
                .Handle<SqlException>(ex => Enum.IsDefined(typeof(RetryableSqlErrors), ex.Number))
                .WaitAndRetry(
                    RetryConstants.RetryCount,
                    retryAttempt => TimeSpan.FromSeconds(ThreadSafeRandom.Next(RetryConstants.MinWait, RetryConstants.MaxWait)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        log.WarnException($"An error has occurred; we will try to re-run the transaction in {timeSpan.TotalMilliseconds} ms. An error has occured {retryCount} times", exception);
                    });

            var retrySqlAsync = Policy
                .Handle<SqlException>(ex => Enum.IsDefined(typeof(RetryableSqlErrors), ex.Number))
                .WaitAndRetryAsync(
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
                    "SQL server errors, such as deadlocks, and retries the command" +
                    "after a short pause"));
            policies.Registry[TransportPolicyDefinitions.RetryCommandHandler] = retrySql;

            //RetryCommandHandlerAsync
            policies.TransportDefinition.TryAdd(TransportPolicyDefinitions.RetryCommandHandlerAsync,
                new TransportPolicyDefinition(
                    TransportPolicyDefinitions.RetryCommandHandler,
                    "A policy for retrying a failed command. This checks specific" +
                    "PostGres server errors, such as deadlocks, and retries the command" +
                    "after a short pause"));
            policies.Registry[TransportPolicyDefinitions.RetryCommandHandlerAsync] = retrySqlAsync;

            //RetryQueryHandler
            policies.TransportDefinition.TryAdd(TransportPolicyDefinitions.RetryQueryHandler,
                new TransportPolicyDefinition(
                    TransportPolicyDefinitions.RetryQueryHandler,
                    "A policy for retrying a failed query. This checks specific" +
                    "SQL server errors, such as deadlocks, and retries the query" +
                    "after a short pause"));
            policies.Registry[TransportPolicyDefinitions.RetryQueryHandler] = retrySql;
        }
    }
}
