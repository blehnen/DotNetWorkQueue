using System;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.Basic.Command;
using DotNetWorkQueue.Transport.LiteDb.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.Tests.Basic
{
    [TestClass]
    public class LiteDbJobQueueCreationTests
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

        private static LiteDbJobQueueCreation CreateJobQueueCreation(ICreationScope creationScope = null)
        {
            var innerCreation = CreateMessageQueueCreation(creationScope);
            return new LiteDbJobQueueCreation(innerCreation);
        }

        private static LiteDbMessageQueueCreation CreateMessageQueueCreation(ICreationScope creationScope = null)
        {
            var connectionInfo = Substitute.For<IConnectionInformation>();
            var queryTableExists = Substitute.For<IQueryHandler<GetTableExistsQuery, bool>>();
            var optionsFactory = Substitute.For<ILiteDbMessageQueueTransportOptionsFactory>();
            optionsFactory.Create().Returns(new LiteDbMessageQueueTransportOptions());
            var createSchema = new LiteDbMessageQueueSchema(optionsFactory);
            var createCommand =
                Substitute.For<ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<ITable>,
                    QueueCreationResult>>();
            var deleteCommand =
                Substitute.For<ICommandHandlerWithOutput<DeleteQueueTablesCommand, QueueRemoveResult>>();
            var scope = creationScope ?? Substitute.For<ICreationScope>();
            var connectionManager = new LiteDbConnectionManager(
                Substitute.For<IConnectionInformation>(),
                Substitute.For<ICreationScope>());

            return new LiteDbMessageQueueCreation(
                connectionInfo,
                queryTableExists,
                optionsFactory,
                createSchema,
                createCommand,
                deleteCommand,
                scope,
                connectionManager);
        }
    }
}
