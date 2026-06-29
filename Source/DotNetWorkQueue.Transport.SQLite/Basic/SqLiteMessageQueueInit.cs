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
using System.Collections.Generic;
using System.Reflection;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Transport.SQLite.Basic.CommandHandler;
using DotNetWorkQueue.Transport.SQLite.Decorator;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <inheritdoc />
    public class SqLiteMessageQueueInit : SqLiteMessageQueueSharedInit
    {
        /// <inheritdoc />
        public override void RegisterImplementations(IContainer container, RegistrationTypes registrationType,
            QueueConnection queueConnection)
        {
            var myType = Assembly.GetAssembly(GetType());
            var baseType = Assembly.GetAssembly(typeof(IDbFactory));

            base.RegisterImplementations(container, registrationType, queueConnection, myType, baseType);

            container.Register<IDbDataSource, DbDataSource>(LifeStyles.Singleton);
            container.Register<IGetFileNameFromConnectionString, GetFileNameFromConnectionString>(LifeStyles.Singleton);
            container.Register<IDbFactory, DbFactory>(LifeStyles.Singleton);
            container.Register<IReaderAsync, ReaderAsync>(LifeStyles.Singleton);
            container.Register<DatabaseExists>(LifeStyles.Singleton);

            container.Register<IWriteMessageHistory, WriteMessageHistoryHandler>(LifeStyles.Singleton);
            container.Register<IQueryMessageHistory, QueryMessageHistoryHandler>(LifeStyles.Singleton);
            container.Register<IPurgeMessageHistory, PurgeMessageHistoryHandler>(LifeStyles.Singleton);
            container.Register<IDbPaginationSyntax, LimitOffsetPaginationSyntax>(LifeStyles.Singleton);

            container.Register<IBaseTransportOptions>(() =>
            {
                try { return (IBaseTransportOptions)container.GetInstance<ITransportOptionsFactory>().Create(); }
                catch { return new SqLiteMessageQueueTransportOptions(); }
            }, LifeStyles.Singleton);

            //command and query retry on transient errors
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

            //some SQLite errors should be warnings, not errors
            container.RegisterDecorator(
                typeof(IQueryHandler<FindMessagesToResetByHeartBeatQuery<long>, IEnumerable<MessageToReset<long>>>),
                typeof(FindRecordsToResetByHeartBeatErrorDecorator), LifeStyles.Singleton);

            //some SQLite errors should be warnings, not errors
            container.RegisterDecorator(
                typeof(IQueryHandler<FindExpiredMessagesToDeleteQuery<long>, IEnumerable<long>>),
                typeof(FindExpiredRecordsToDeleteQueryHandlerErrorDecorator), LifeStyles.Singleton);

            //true bulk-insert batch send handlers; override the relational no-op fallback so
            //SendMessages<long> dispatches batches to a real handler for SQLite
            container.Register<ISendMessageBatchSupport>(() => new SendMessageBatchSupport(true), LifeStyles.Singleton);
            container.Register<ICommandHandlerWithOutput<SendMessageCommandBatch, QueueOutputMessages>,
                SendMessageCommandBatchHandler>(LifeStyles.Singleton);
            container.Register<ICommandHandlerWithOutputAsync<SendMessageCommandBatch, QueueOutputMessages>,
                SendMessageCommandBatchHandlerAsync>(LifeStyles.Singleton);
        }

        /// <inheritdoc />
        protected override void SetupPolicy(IContainer container)
        {
            RetryTransactionPolicyCreation.Register(container);
        }
    }
}
