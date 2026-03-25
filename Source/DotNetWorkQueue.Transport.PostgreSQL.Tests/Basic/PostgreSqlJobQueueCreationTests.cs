using System;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic
{
    [TestClass]
    public class PostgreSqlJobQueueCreationTests
    {
        [TestMethod]
        public void Create_Default()
        {
            using (var creation = CreateJobQueueCreation())
            {
                Assert.IsNotNull(creation);
            }
        }

        [TestMethod]
        public void IsDisposed_False_By_Default()
        {
            using (var creation = CreateJobQueueCreation())
            {
                Assert.IsFalse(creation.IsDisposed);
            }
        }

        [TestMethod]
        public void Scope_Returns_Inner_Scope()
        {
            var expectedScope = Substitute.For<ICreationScope>();
            using (var creation = CreateJobQueueCreation(creationScope: expectedScope))
            {
                Assert.AreSame(expectedScope, creation.Scope);
            }
        }

        [TestMethod]
        public void IsDisposed_True_After_Dispose()
        {
            var creation = CreateJobQueueCreation();
            creation.Dispose();
            Assert.IsTrue(creation.IsDisposed);
        }

        [TestMethod]
        public void Dispose_Is_Idempotent()
        {
            var creation = CreateJobQueueCreation();
            creation.Dispose();
            creation.Dispose(); // Should not throw
        }

        [TestMethod]
        public void Options_Returns_Value()
        {
            using (var creation = CreateJobQueueCreation())
            {
                Assert.IsNotNull(creation.Options);
            }
        }

        private static PostgreSqlJobQueueCreation CreateJobQueueCreation(ICreationScope creationScope = null)
        {
            var innerCreation = CreateMessageQueueCreation(creationScope);
            return new PostgreSqlJobQueueCreation(innerCreation);
        }

        private static PostgreSqlMessageQueueCreation CreateMessageQueueCreation(ICreationScope creationScope = null)
        {
            var connectionInfo = Substitute.For<IConnectionInformation>();
            var queryTableExists = Substitute.For<IQueryHandler<GetTableExistsQuery, bool>>();
            var optionsFactory = Substitute.For<IPostgreSqlMessageQueueTransportOptionsFactory>();
            optionsFactory.Create().Returns(new PostgreSqlMessageQueueTransportOptions());
            var tableNameHelper = Substitute.For<ITableNameHelper>();
            var createSchema = new PostgreSqlMessageQueueSchema(tableNameHelper, optionsFactory);
            var createCommand =
                Substitute.For<ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<ITable>,
                    QueueCreationResult>>();
            var deleteCommand =
                Substitute.For<ICommandHandlerWithOutput<DeleteQueueTablesCommand, QueueRemoveResult>>();
            var scope = creationScope ?? Substitute.For<ICreationScope>();

            return new PostgreSqlMessageQueueCreation(
                connectionInfo,
                queryTableExists,
                optionsFactory,
                createSchema,
                createCommand,
                deleteCommand,
                scope);
        }
    }
}
