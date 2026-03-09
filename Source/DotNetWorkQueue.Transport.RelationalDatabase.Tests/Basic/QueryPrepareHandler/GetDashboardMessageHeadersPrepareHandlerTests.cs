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
    public class GetDashboardMessageHeadersPrepareHandlerTests
    {
        [TestMethod]
        public void Handle_Sets_CommandText()
        {
            var handler = CreateHandler();
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardMessageHeadersQuery("42"), command, CommandStringTypes.GetDashboardMessageHeaders);

            Assert.IsNotNull(command.CommandText);
        }

        [TestMethod]
        public void Handle_Adds_QueueId_Parameter()
        {
            var handler = CreateHandler();
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardMessageHeadersQuery("42"), command, CommandStringTypes.GetDashboardMessageHeaders);

            var parameters = (DataParameterCollection)command.Parameters;
            var param = parameters.First();
            Assert.AreEqual("@QueueId", param.ParameterName);
            Assert.AreEqual(42L, param.Value);
        }

        private static GetDashboardMessageHeadersPrepareHandler CreateHandler()
        {
            var cache = new FakeCommandStringCache();
            return new GetDashboardMessageHeadersPrepareHandler(cache);
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
