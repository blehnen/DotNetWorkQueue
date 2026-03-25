using System;
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic
{
    [TestClass]
    public class DashboardRequeueAllErrorMessagesCommandHandlerTests
    {
        [TestMethod]
        public void Create_Null_OptionsFactory_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new DashboardRequeueAllErrorMessagesCommandHandler(
                    null,
                    Substitute.For<IDbConnectionFactory>(),
                    Substitute.For<ITransactionFactory>(),
                    Substitute.For<IPrepareCommandHandler<DashboardRequeueAllErrorMessagesCommand>>()));
        }

        [TestMethod]
        public void Create_Null_ConnectionFactory_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new DashboardRequeueAllErrorMessagesCommandHandler(
                    Substitute.For<ITransportOptionsFactory>(),
                    null,
                    Substitute.For<ITransactionFactory>(),
                    Substitute.For<IPrepareCommandHandler<DashboardRequeueAllErrorMessagesCommand>>()));
        }

        [TestMethod]
        public void Create_Null_TransactionFactory_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new DashboardRequeueAllErrorMessagesCommandHandler(
                    Substitute.For<ITransportOptionsFactory>(),
                    Substitute.For<IDbConnectionFactory>(),
                    null,
                    Substitute.For<IPrepareCommandHandler<DashboardRequeueAllErrorMessagesCommand>>()));
        }

        [TestMethod]
        public void Create_Null_PrepareCommand_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new DashboardRequeueAllErrorMessagesCommandHandler(
                    Substitute.For<ITransportOptionsFactory>(),
                    Substitute.For<IDbConnectionFactory>(),
                    Substitute.For<ITransactionFactory>(),
                    null));
        }

        [TestMethod]
        public void Create_Default()
        {
            var handler = CreateHandler();
            Assert.IsNotNull(handler);
        }

        [TestMethod]
        public void Handle_StatusTable_Disabled_Executes_Three_Commands()
        {
            var (handler, prepareCommand, dbCommand) = CreateHandlerWithMocks(enableStatusTable: false);
            dbCommand.ExecuteNonQuery().Returns(5);

            var result = handler.Handle(new DashboardRequeueAllErrorMessagesCommand());

            Assert.AreEqual(5, result);
            // Should call Handle 3 times: DashboardRequeueAllErrors, ErrorTracking, MetaDataErrors
            prepareCommand.Received(3).Handle(
                Arg.Any<DashboardRequeueAllErrorMessagesCommand>(),
                Arg.Any<IDbCommand>(),
                Arg.Any<CommandStringTypes>());
        }

        [TestMethod]
        public void Handle_StatusTable_Enabled_Executes_Four_Commands()
        {
            var (handler, prepareCommand, dbCommand) = CreateHandlerWithMocks(enableStatusTable: true);
            dbCommand.ExecuteNonQuery().Returns(2);

            var result = handler.Handle(new DashboardRequeueAllErrorMessagesCommand());

            Assert.AreEqual(2, result);
            // Should call Handle 4 times: DashboardRequeueAllErrors, ErrorTracking, StatusTable, MetaDataErrors
            prepareCommand.Received(4).Handle(
                Arg.Any<DashboardRequeueAllErrorMessagesCommand>(),
                Arg.Any<IDbCommand>(),
                Arg.Any<CommandStringTypes>());
        }

        [TestMethod]
        public void Handle_Opens_Connection()
        {
            var (handler, _, _) = CreateHandlerWithMocks(enableStatusTable: false, out var connection, out _, out _);
            handler.Handle(new DashboardRequeueAllErrorMessagesCommand());
            connection.Received(1).Open();
        }

        [TestMethod]
        public void Handle_Commits_Transaction()
        {
            var (handler, _, _) = CreateHandlerWithMocks(enableStatusTable: false, out _, out var transaction, out _);
            handler.Handle(new DashboardRequeueAllErrorMessagesCommand());
            transaction.Received(1).Commit();
        }

        [TestMethod]
        public void Handle_Returns_Count_From_First_ExecuteNonQuery()
        {
            var (handler, _, dbCommand) = CreateHandlerWithMocks(enableStatusTable: false);
            var callCount = 0;
            dbCommand.ExecuteNonQuery().Returns(_ =>
            {
                callCount++;
                return callCount == 1 ? 7 : 0;
            });

            var result = handler.Handle(new DashboardRequeueAllErrorMessagesCommand());
            Assert.AreEqual(7, result);
        }

        [TestMethod]
        public void Handle_Calls_PrepareCommand_With_Correct_CommandStringTypes()
        {
            var (handler, prepareCommand, _) = CreateHandlerWithMocks(enableStatusTable: true);

            handler.Handle(new DashboardRequeueAllErrorMessagesCommand());

            prepareCommand.Received(1).Handle(
                Arg.Any<DashboardRequeueAllErrorMessagesCommand>(),
                Arg.Any<IDbCommand>(),
                CommandStringTypes.DashboardRequeueAllErrors);
            prepareCommand.Received(1).Handle(
                Arg.Any<DashboardRequeueAllErrorMessagesCommand>(),
                Arg.Any<IDbCommand>(),
                CommandStringTypes.DashboardRequeueAllErrors_ErrorTracking);
            prepareCommand.Received(1).Handle(
                Arg.Any<DashboardRequeueAllErrorMessagesCommand>(),
                Arg.Any<IDbCommand>(),
                CommandStringTypes.DashboardRequeueAllErrors_StatusTable);
            prepareCommand.Received(1).Handle(
                Arg.Any<DashboardRequeueAllErrorMessagesCommand>(),
                Arg.Any<IDbCommand>(),
                CommandStringTypes.DashboardRequeueAllErrors_MetaDataErrors);
        }

        [TestMethod]
        public void Handle_StatusTable_Disabled_Does_Not_Call_StatusTable_Command()
        {
            var (handler, prepareCommand, _) = CreateHandlerWithMocks(enableStatusTable: false);

            handler.Handle(new DashboardRequeueAllErrorMessagesCommand());

            prepareCommand.DidNotReceive().Handle(
                Arg.Any<DashboardRequeueAllErrorMessagesCommand>(),
                Arg.Any<IDbCommand>(),
                CommandStringTypes.DashboardRequeueAllErrors_StatusTable);
        }

        private static DashboardRequeueAllErrorMessagesCommandHandler CreateHandler()
        {
            var optionsFactory = Substitute.For<ITransportOptionsFactory>();
            var options = Substitute.For<ITransportOptions>();
            optionsFactory.Create().Returns(options);
            return new DashboardRequeueAllErrorMessagesCommandHandler(
                optionsFactory,
                Substitute.For<IDbConnectionFactory>(),
                Substitute.For<ITransactionFactory>(),
                Substitute.For<IPrepareCommandHandler<DashboardRequeueAllErrorMessagesCommand>>());
        }

        private static (DashboardRequeueAllErrorMessagesCommandHandler handler,
            IPrepareCommandHandler<DashboardRequeueAllErrorMessagesCommand> prepareCommand,
            IDbCommand dbCommand)
            CreateHandlerWithMocks(bool enableStatusTable)
        {
            var (handler, prepareCommand, dbCommand) = CreateHandlerWithMocks(enableStatusTable, out _, out _, out _);
            return (handler, prepareCommand, dbCommand);
        }

        private static (DashboardRequeueAllErrorMessagesCommandHandler handler,
            IPrepareCommandHandler<DashboardRequeueAllErrorMessagesCommand> prepareCommand,
            IDbCommand dbCommand)
            CreateHandlerWithMocks(bool enableStatusTable,
                out IDbConnection connection,
                out IDbTransaction transaction,
                out ITransactionWrapper transactionWrapper)
        {
            var optionsFactory = Substitute.For<ITransportOptionsFactory>();
            var options = Substitute.For<ITransportOptions>();
            options.EnableStatusTable.Returns(enableStatusTable);
            optionsFactory.Create().Returns(options);

            connection = Substitute.For<IDbConnection>();
            var dbCommand = Substitute.For<IDbCommand>();
            connection.CreateCommand().Returns(dbCommand);

            transaction = Substitute.For<IDbTransaction>();
            transactionWrapper = Substitute.For<ITransactionWrapper>();
            transactionWrapper.BeginTransaction().Returns(transaction);

            var transactionFactory = Substitute.For<ITransactionFactory>();
            transactionFactory.Create(connection).Returns(transactionWrapper);

            var dbConnectionFactory = Substitute.For<IDbConnectionFactory>();
            dbConnectionFactory.Create().Returns(connection);

            var prepareCommand = Substitute.For<IPrepareCommandHandler<DashboardRequeueAllErrorMessagesCommand>>();

            var handler = new DashboardRequeueAllErrorMessagesCommandHandler(
                optionsFactory, dbConnectionFactory, transactionFactory, prepareCommand);
            return (handler, prepareCommand, dbCommand);
        }
    }
}
