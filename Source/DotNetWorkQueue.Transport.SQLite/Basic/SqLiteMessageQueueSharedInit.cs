// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using System.Data;
using System.Reflection;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Transport.SQLite.Basic.CommandPrepareHandler;
using DotNetWorkQueue.Transport.SQLite.Basic.Factory;
using DotNetWorkQueue.Transport.SQLite.Basic.Message;
using DotNetWorkQueue.Transport.SQLite.Basic.QueryHandler;
using DotNetWorkQueue.Transport.SQLite.Decorator;
using DotNetWorkQueue.Validation;
using FindExpiredRecordsToDeleteQueryPrepareHandler = DotNetWorkQueue.Transport.SQLite.Basic.QueryPrepareHandler.FindExpiredRecordsToDeleteQueryPrepareHandler;
using FindRecordsToResetByHeartBeatQueryPrepareHandler = DotNetWorkQueue.Transport.SQLite.Basic.QueryPrepareHandler.FindRecordsToResetByHeartBeatQueryPrepareHandler;
using FindErrorRecordsToDeleteQueryPrepareHandler = DotNetWorkQueue.Transport.SQLite.Basic.QueryPrepareHandler.FindErrorRecordsToDeleteQueryPrepareHandler;
using ReceiveMessage = DotNetWorkQueue.Transport.SQLite.Basic.Message.ReceiveMessage;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// Registers the implementations for the SQLite queue into the IoC container.
    /// </summary>
    public class SqLiteMessageQueueSharedInit : TransportMessageQueueSharedInit
    {
        /// <summary>
        /// Allows a transport to register its dependencies in the IoC container.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="registrationType">Type of the registration.</param>
        /// <param name="queueConnection">Queue and connection information.</param>
        public override void RegisterImplementations(IContainer container, RegistrationTypes registrationType,
            QueueConnection queueConnection)
        {
            RegisterImplementations(container, registrationType, queueConnection, Assembly.GetAssembly(GetType()));
        }


        /// <summary>
        /// Registers the implementations.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="registrationType">Type of the registration.</param>
        /// <param name="queueConnection">Queue and connection information.</param>
        /// <param name="assemblies">The assemblies.</param>
        public virtual void RegisterImplementations(IContainer container, RegistrationTypes registrationType,
            QueueConnection queueConnection, params Assembly[] assemblies)
        {
            Guard.NotNull(() => container, container);
            base.RegisterImplementations(container, registrationType, queueConnection);
            var init = new RelationalDatabaseMessageQueueInit<long, Guid>();
            init.RegisterStandardImplementations(container, assemblies);

            //**all
            container.RegisterNonScopedSingleton<ICreationScope>(new CreationScope());

            container.Register<SqLiteMessageQueueSchema>(LifeStyles.Singleton);
            container.Register<IQueueCreation, SqLiteMessageQueueCreation>(LifeStyles.Singleton);
            container.Register<IJobSchedulerLastKnownEvent, SqliteJobSchedulerLastKnownEvent>(LifeStyles.Singleton);
            container.Register<ISendJobToQueue, SqliteSendToJobQueue>(LifeStyles.Singleton);
            container.Register<IDbConnectionFactory, DbConnectionFactory>(LifeStyles.Singleton);
            container.Register<SqliteJobSchema>(LifeStyles.Singleton);
            container.Register<IOptionsSerialization, OptionsSerialization>(LifeStyles.Singleton);
            container.Register<CommandStringCache, IDbCommandStringCache>(LifeStyles.Singleton);
            container.Register<ITransportOptionsFactory, TransportOptionsFactory>(LifeStyles.Singleton);
            container.Register<ITransactionFactory, TransactionFactory>(LifeStyles.Singleton);
            container.Register<IJobSchema, SqliteJobSchema>(LifeStyles.Singleton);
            container.Register<IReadColumn, ReadColumn>(LifeStyles.Singleton);
            container.Register<IBuildMoveToErrorQueueSql, BuildMoveToErrorQueueSql>(LifeStyles.Singleton);
            container.Register<IGetPreviousMessageErrors, GetPreviousMessageErrors<long>>(LifeStyles.Singleton);

            container.Register<IGetFirstMessageDeliveryTime, GetFirstMessageDeliveryTime>(LifeStyles.Singleton);
            container
                .Register
                <ISqLiteMessageQueueTransportOptionsFactory, SqLiteMessageQueueTransportOptionsFactory>(
                    LifeStyles.Singleton);

            container.Register<IDbCommandStringCache>(LifeStyles.Singleton);

            // Phase 5: use the symmetric-normalization wrapper so the validator
            // (ExternalTransactionValidator) compares both sides under identical
            // canonicalization. Wrapper applies Path.GetFullPath + ToUpperInvariant
            // with a :memory: short-circuit, matching SqLiteExternalDbNameExtractor.
            container.Register<IConnectionInformation>(
                () => new SqliteNormalizedConnectionInformation(queueConnection, container.GetInstance<IDbDataSource>()),
                LifeStyles.Singleton);

            container.Register<BuildDequeueCommand>(LifeStyles.Singleton);
            container.Register<MessageDeQueue>(LifeStyles.Singleton);

            container.Register<SqLiteMessageQueueTransportOptions>(LifeStyles.Singleton);

            container
                .Register<IConnectionHeader<IDbConnection, IDbTransaction, IDbCommand>,
                    ConnectionHeader<IDbConnection, IDbTransaction, IDbCommand>>(LifeStyles.Singleton);

            // Phase 5: outbox-pattern producer wiring (SQLite side — sweep). Mirrors the
            // SqlServer / PostgreSQL outbox-milestone init blocks. ExternalTransactionValidator
            // verifies the caller's tx is on the same DB as the queue; SqLiteExternalDbNameExtractor
            // provides the symmetric-normalized DB-name comparison input.
            container.Register<IExternalDbNameExtractor, SqLiteExternalDbNameExtractor>(LifeStyles.Singleton);
            container.Register<ExternalTransactionValidator>(LifeStyles.Singleton);
            container.RegisterConditional(typeof(IProducerQueue<>), typeof(SqLiteRelationalProducerQueue<>), LifeStyles.Singleton);
            container.RegisterConditional(typeof(IRelationalProducerQueue<>), typeof(SqLiteRelationalProducerQueue<>), LifeStyles.Singleton);
            container.RegisterConditional(typeof(RelationalProducerQueue<>), typeof(SqLiteRelationalProducerQueue<>), LifeStyles.Singleton);

            // Phase 5: SQLite hold-transaction state header (carries the per-message dequeue
            // connection + transaction across the receive-path / user-handler boundary when
            // EnableHoldTransactionUntilMessageCommitted = true). Set in ReceiveMessage.GetMessage;
            // read by SqLiteMessageQueueReceive commit / rollback / cleanup delegates and by the
            // inbox SqLiteRelationalWorkerNotification.Transaction getter.
            container.Register<SqLiteHeaders>(LifeStyles.Singleton);

            // Phase 5: inbox-pattern receive wiring (SQLite side).
            // Pre-register the relational concrete; WorkerNotification is already registered by
            // the core (ComponentRegistration line 217) and auto-resolvable as a concrete type.
            // The IWorkerNotification binding branches on EnableHoldTransactionUntilMessageCommitted:
            // option=true returns SqLiteRelationalWorkerNotification (which implements
            // IRelationalWorkerNotification, exposing the held DbTransaction via the
            // capability-cast pattern); option=false returns plain WorkerNotification (cast
            // cleanly fails).
            // The try/catch around options resolution mirrors the IBaseTransportOptions pattern
            // (Phase 3 lesson 1) — at container.Verify() / early-resolution time options may not
            // be loadable yet, so fall back to the default option value (false).
            container.Register<SqLiteRelationalWorkerNotification>(LifeStyles.Transient);
            container.Register<IWorkerNotification>(() =>
            {
                bool holdTransaction;
                try
                {
                    var optionsFactory = container.GetInstance<ITransportOptionsFactory>();
                    var options = (SqLiteMessageQueueTransportOptions)optionsFactory.Create();
                    holdTransaction = options.EnableHoldTransactionUntilMessageCommitted;
                }
                catch
                {
                    holdTransaction = false;
                }
                return holdTransaction
                    ? (IWorkerNotification)container.GetInstance<SqLiteRelationalWorkerNotification>()
                    : container.GetInstance<WorkerNotification>();
            }, LifeStyles.Transient);

            container.Register<ISQLiteTransactionWrapper, SqLiteTransactionWrapper>(LifeStyles.Transient);
            //**all

            //**receive
            container.Register<IReceiveMessages, SqLiteMessageQueueReceive>(LifeStyles.Transient);
            container.Register<ITransportRollbackMessage, RollbackMessage>(LifeStyles.Singleton);
            container.Register<ReceiveMessage>(LifeStyles.Transient);
            //**receive

            //reset heart beat 
            container
                .Register<IPrepareCommandHandler<ResetHeartBeatCommand<long>>,
                    ResetHeartBeatCommandPrepareHandler>(LifeStyles.Singleton);

            container
                .Register<IPrepareCommandHandler<MoveRecordToErrorQueueCommand<long>>,
                    MoveRecordToErrorQueueCommandPrepareHandler>(LifeStyles.Singleton);

            //explicit registration of our job exists query
            container
                .Register<IQueryHandler<DoesJobExistQuery<IDbConnection, IDbTransaction>,
                        QueueStatuses>,
                    DoesJobExistQueryHandler<IDbConnection, IDbTransaction>>(LifeStyles.Singleton);

            //because we have an explicit registration for job exists, we need to explicitly register the prepare statement
            container
                .Register<IPrepareQueryHandler<DoesJobExistQuery<IDbConnection, IDbTransaction>,
                        QueueStatuses>,
                    DoesJobExistQueryPrepareHandler<IDbConnection, IDbTransaction>>(LifeStyles.Singleton);

            container
                .Register<ICommandHandler<MoveRecordToErrorQueueCommand<long>>,
                    MoveRecordToErrorQueueCommandHandler<IDbConnection, IDbTransaction, IDbCommand>>(LifeStyles
                    .Singleton);

            //expired messages
            container
                .Register<IPrepareQueryHandler<FindExpiredMessagesToDeleteQuery<long>, IEnumerable<long>>,
                    FindExpiredRecordsToDeleteQueryPrepareHandler>(LifeStyles.Singleton);

            //error messages
            container
                .Register<IPrepareQueryHandler<FindErrorMessagesToDeleteQuery<long>, IEnumerable<long>>,
                    FindErrorRecordsToDeleteQueryPrepareHandler>(LifeStyles.Singleton);

            //heartbeat
            container
                .Register<IPrepareQueryHandler<FindMessagesToResetByHeartBeatQuery<long>, IEnumerable<MessageToReset<long>>>,
                    FindRecordsToResetByHeartBeatQueryPrepareHandler>(LifeStyles.Singleton);

            //explicit registration of options
            container
                .Register<IQueryHandler<GetQueueOptionsQuery<SqLiteMessageQueueTransportOptions>,
                        SqLiteMessageQueueTransportOptions>,
                    GetQueueOptionsQueryHandler<SqLiteMessageQueueTransportOptions>>(LifeStyles.Singleton);

            container
                .Register<IPrepareQueryHandler<GetQueueOptionsQuery<SqLiteMessageQueueTransportOptions>,
                        SqLiteMessageQueueTransportOptions>,
                    GetQueueOptionsQueryPrepareHandler<SqLiteMessageQueueTransportOptions>>(LifeStyles.Singleton);

            container.RegisterDecorator(typeof(ISQLiteTransactionWrapper),
                typeof(BeginTransactionRetryDecorator), LifeStyles.Transient);

            //register our decorator for deleting messages
            container.RegisterDecorator(
                typeof(ICommandHandlerWithOutput<DeleteMessageCommand<long>, long>),
                typeof(DeleteMessageCommandDecorator), LifeStyles.Singleton);

            //register our decorator for setting the status
            container.RegisterDecorator(
                typeof(ICommandHandler<DeleteStatusTableStatusCommand<long>>),
                typeof(SetStatusTableStatusCommandDecorator), LifeStyles.Singleton);

            //register our decorator for resetting the heart beat
            container.RegisterDecorator(
                typeof(ICommandHandlerWithOutput<ResetHeartBeatCommand<long>, long>),
                typeof(ResetHeartBeatCommandDecorator), LifeStyles.Singleton);

            container.RegisterDecorator(
                typeof(ICommandHandler<MoveRecordToErrorQueueCommand<long>>),
                typeof(MoveRecordToErrorQueueCommandDecorator), LifeStyles.Singleton);

            container.RegisterDecorator(
                typeof(IQueryHandler<GetErrorRecordExistsQuery<long>, bool>),
                typeof(GetErrorRecordExistsQueryDecorator), LifeStyles.Singleton);

            container.RegisterDecorator(
                typeof(IQueryHandler<FindExpiredMessagesToDeleteQuery<long>, IEnumerable<long>>),
                typeof(FindExpiredRecordsToDeleteDecorator), LifeStyles.Singleton);

            container.RegisterDecorator(
                typeof(IQueryHandler<FindMessagesToResetByHeartBeatQuery<long>, IEnumerable<MessageToReset<long>>>),
                typeof(FindRecordsToResetByHeartBeatDecorator), LifeStyles.Singleton);

            container.RegisterDecorator(
                typeof(IQueryHandler<GetColumnNamesFromTableQuery, List<string>>),
                typeof(GetColumnNamesFromTableDecorator), LifeStyles.Singleton);

            container.RegisterDecorator(
                typeof(IQueryHandler<GetTableExistsQuery, bool>),
                typeof(GetTableExistsDecorator), LifeStyles.Singleton);

            container.RegisterDecorator(
                typeof(IQueryHandler<DoesJobExistQuery<IDbConnection, IDbTransaction>, QueueStatuses>),
                typeof(DoesJobExistDecorator), LifeStyles.Singleton);

            container.RegisterDecorator(
                typeof(ICommandHandlerWithOutput<DeleteQueueTablesCommand, QueueRemoveResult>),
                typeof(DeleteQueueTablesDecorator), LifeStyles.Singleton);

            container.RegisterDecorator(
                typeof(ICommandHandler<SetErrorCountCommand<long>>),
                typeof(SetErrorCountCommandDecorator), LifeStyles.Singleton);

            //trace fallback command
            container.RegisterDecorator(
                typeof(ICommandHandler<RollbackMessageCommand<long>>),
                typeof(DotNetWorkQueue.Transport.SQLite.Trace.Decorator.RollbackMessageCommandHandlerDecorator), LifeStyles.Singleton);

            //trace sending a message so that we can add specific tags
            container.RegisterDecorator(
                typeof(ICommandHandlerWithOutput<SendMessageCommand, long>),
                typeof(DotNetWorkQueue.Transport.SQLite.Trace.Decorator.SendMessageCommandHandlerDecorator), LifeStyles.Singleton);

            container.RegisterDecorator(
                typeof(ICommandHandlerWithOutputAsync<SendMessageCommand, long>),
                typeof(DotNetWorkQueue.Transport.SQLite.Trace.Decorator.SendMessageCommandHandlerAsyncDecorator), LifeStyles.Singleton);
        }

        /// <summary>
        /// Allows the transport to set default configuration settings or other values
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="registrationType">Type of the registration.</param>
        /// <param name="connectionType">Type of the connection.</param>
        public override void SetDefaultsIfNeeded(IContainer container, RegistrationTypes registrationType,
            ConnectionTypes connectionType)
        {
            SetupPolicy(container);

            var init = new RelationalDatabaseMessageQueueInit<long, Guid>();
            init.SetDefaultsIfNeeded(container, "SQLiteMessageQueueTransportOptions",
                "SQLiteMessageQueueTransportOptions");

            //create in memory hold
            var getFileName = container.GetInstance<IGetFileNameFromConnectionString>();
            var connection = container.GetInstance<IConnectionInformation>();
            var fileName = getFileName.GetFileName(connection.ConnectionString);
            if (!fileName.IsInMemory) return;
            var scope = container.GetInstance<ICreationScope>();
            var holder = new SqLiteHoldConnection(getFileName, container.GetInstance<IDbFactory>());
            holder.AddConnectionIfNeeded(connection);
            scope.AddScopedObject(holder);
        }

        /// <summary>
        /// Setup the policies for a transport
        /// </summary>
        /// <param name="container">The container.</param>
        protected virtual void SetupPolicy(IContainer container)
        { //no-op unless overridden
        }

        /// <inheritdoc />
        public override bool IsRelationalTransport => true;
    }
}
