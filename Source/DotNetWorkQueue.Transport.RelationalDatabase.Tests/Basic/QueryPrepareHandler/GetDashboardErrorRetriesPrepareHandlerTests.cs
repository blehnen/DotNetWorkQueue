using System.Data;
using System.Linq;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryPrepareHandler
{
    [TestClass]
    public class GetDashboardErrorRetriesPrepareHandlerTests
    {
        [TestMethod]
        public void Handle_Sets_CommandText()
        {
            var cache = new FakeCommandStringCache();
            var handler = new GetDashboardErrorRetriesPrepareHandler(cache);
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardErrorRetriesQuery("42"), command, CommandStringTypes.GetDashboardErrorRetries);

            Assert.AreEqual("SELECT retries", command.CommandText);
        }

        [TestMethod]
        public void Handle_Adds_QueueId_Parameter()
        {
            var cache = new FakeCommandStringCache();
            var handler = new GetDashboardErrorRetriesPrepareHandler(cache);
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardErrorRetriesQuery("42"), command, CommandStringTypes.GetDashboardErrorRetries);

            var parameters = (DataParameterCollection)command.Parameters;
            var param = parameters.First();
            Assert.AreEqual("@QueueId", param.ParameterName);
            Assert.AreEqual(42L, param.Value);
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
