using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryPrepareHandler
{
    [TestClass]
    public class GetDashboardErrorMessageCountPrepareHandlerTests
    {
        [TestMethod]
        public void Handle_Sets_CommandText()
        {
            var cache = new FakeCommandStringCache();
            var handler = new GetDashboardErrorMessageCountPrepareHandler(cache);
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardErrorMessageCountQuery(), command, CommandStringTypes.GetDashboardErrorMessageCount);

            Assert.AreEqual("SELECT COUNT(*) FROM errors", command.CommandText);
        }

        [TestMethod]
        public void Handle_Adds_No_Parameters()
        {
            var cache = new FakeCommandStringCache();
            var handler = new GetDashboardErrorMessageCountPrepareHandler(cache);
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardErrorMessageCountQuery(), command, CommandStringTypes.GetDashboardErrorMessageCount);

            Assert.IsEmpty(command.Parameters);
        }

        private static IDbCommand CreateDbCommand()
        {
            var command = Substitute.For<IDbCommand>();
            var parameters = new DataParameterCollection();
            command.Parameters.Returns(parameters);
            command.CreateParameter().Returns(_ => Substitute.For<IDbDataParameter>());
            return command;
        }
    }
}
