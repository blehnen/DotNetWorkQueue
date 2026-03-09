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
    public class GetDashboardErrorMessagesPrepareHandlerTests
    {
        [TestMethod]
        public void Handle_Sets_CommandText()
        {
            var cache = new FakeCommandStringCache();
            var handler = new GetDashboardErrorMessagesPrepareHandler(cache);
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardErrorMessagesQuery(0, 25), command, CommandStringTypes.GetDashboardErrorMessages);

            Assert.AreEqual("SELECT errors", command.CommandText);
        }

        [TestMethod]
        public void Handle_Adds_Offset_And_PageSize_Parameters()
        {
            var cache = new FakeCommandStringCache();
            var handler = new GetDashboardErrorMessagesPrepareHandler(cache);
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardErrorMessagesQuery(1, 50), command, CommandStringTypes.GetDashboardErrorMessages);

            var parameters = (DataParameterCollection)command.Parameters;
            Assert.AreEqual(2, parameters.Count);
            Assert.IsTrue(parameters.Any(p => p.ParameterName == "@Offset"));
            Assert.IsTrue(parameters.Any(p => p.ParameterName == "@PageSize"));
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
