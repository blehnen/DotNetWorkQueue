using System.Collections.Generic;
using System.Reflection;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.SQLite.Decorator;
using DotNetWorkQueue.Transport.SQLite.Shared;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <inheritdoc />
    public class SqLiteMessageQueueInit : SqLiteMessageQueueSharedInit
    {
        /// <inheritdoc />
        public override void RegisterImplementations(IContainer container, RegistrationTypes registrationType,
            string connection, string queue)
        {
            var myType = Assembly.GetAssembly(GetType());
            var baseType = Assembly.GetAssembly(typeof(IDbFactory));

            base.RegisterImplementations(container, registrationType, connection, queue, myType, baseType);

            container.Register<IDbDataSource, DbDataSource>(LifeStyles.Singleton);
            container.Register<IGetFileNameFromConnectionString, GetFileNameFromConnectionString>(LifeStyles.Singleton);
            container.Register<IDbFactory, DbFactory>(LifeStyles.Singleton);
            container.Register<IReaderAsync, ReaderAsync>(LifeStyles.Singleton);
            container.Register<DatabaseExists>(LifeStyles.Singleton);

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
                typeof(RelationalDatabase.IQueryHandler<FindMessagesToResetByHeartBeatQuery, IEnumerable<MessageToReset>>),
                typeof(FindRecordsToResetByHeartBeatErrorDecorator), LifeStyles.Singleton);

            //some SQLite errors should be warnings, not errors
            container.RegisterDecorator(
                typeof(RelationalDatabase.IQueryHandler<FindExpiredMessagesToDeleteQuery, IEnumerable<long>>),
                typeof(FindExpiredRecordsToDeleteQueryHandlerErrorDecorator), LifeStyles.Singleton);
        }

        /// <inheritdoc />
        protected override void SetupPolicy(IContainer container)
        {
            RetryTransactionPolicyCreation.Register(container);
        }
    }
}
