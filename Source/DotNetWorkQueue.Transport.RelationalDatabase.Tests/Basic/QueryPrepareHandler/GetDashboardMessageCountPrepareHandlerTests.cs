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
    public class GetDashboardMessageCountPrepareHandlerTests
    {
        [TestMethod]
        public void Handle_Sets_CommandText_Without_Filter()
        {
            var cache = new FakeCommandStringCache();
            var handler = new GetDashboardMessageCountPrepareHandler(cache);
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardMessageCountQuery(null), command, CommandStringTypes.GetDashboardMessageCount);

            Assert.AreEqual("SELECT COUNT(*) FROM meta", command.CommandText);
        }

        [TestMethod]
        public void Handle_No_Parameters_Without_Filter()
        {
            var cache = new FakeCommandStringCache();
            var handler = new GetDashboardMessageCountPrepareHandler(cache);
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardMessageCountQuery(null), command, CommandStringTypes.GetDashboardMessageCount);

            Assert.IsEmpty(((DataParameterCollection)command.Parameters));
        }

        [TestMethod]
        public void Handle_Adds_Status_Parameter_When_Filter_Set()
        {
            var cache = new FakeCommandStringCache();
            var handler = new GetDashboardMessageCountPrepareHandler(cache);
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardMessageCountQuery(1), command, CommandStringTypes.GetDashboardMessageCount);

            var parameters = (DataParameterCollection)command.Parameters;
            Assert.IsTrue(parameters.Any(p => p.ParameterName == "@Status"));
            StringAssert.Contains(command.CommandText, "WHERE Status = @Status");
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
