using System;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic
{
    [TestClass]
    public class SqlServerJobQueueCreationTests
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

        private static SqlServerJobQueueCreation CreateJobQueueCreation(ICreationScope creationScope = null)
        {
            var innerCreation = CreateMessageQueueCreation(creationScope);
            return new SqlServerJobQueueCreation(innerCreation);
        }

        private static SqlServerMessageQueueCreation CreateMessageQueueCreation(ICreationScope creationScope = null)
        {
            var connectionInfo = Substitute.For<IConnectionInformation>();
            var queryTableExists = Substitute.For<IQueryHandler<GetTableExistsQuery, bool>>();
            var optionsFactory = Substitute.For<ISqlServerMessageQueueTransportOptionsFactory>();
            optionsFactory.Create().Returns(new SqlServerMessageQueueTransportOptions());
            var tableNameHelper = new TableNameHelper(connectionInfo);
            var sqlSchema = Substitute.For<ISqlSchema>();
            var createSchema = new SqlServerMessageQueueSchema(tableNameHelper, optionsFactory, sqlSchema);
            var createCommand =
                Substitute.For<ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<ITable>,
                    QueueCreationResult>>();
            var deleteCommand =
                Substitute.For<ICommandHandlerWithOutput<DeleteQueueTablesCommand, QueueRemoveResult>>();
            var scope = creationScope ?? Substitute.For<ICreationScope>();

            return new SqlServerMessageQueueCreation(
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
