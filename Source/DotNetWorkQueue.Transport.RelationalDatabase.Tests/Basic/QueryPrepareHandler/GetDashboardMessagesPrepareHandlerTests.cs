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
    public class GetDashboardMessagesPrepareHandlerTests
    {
        [TestMethod]
        public void Handle_Sets_CommandText()
        {
            var handler = CreateHandler();
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardMessagesQuery(0, 25, null), command, CommandStringTypes.GetDashboardMessages);

            Assert.IsNotNull(command.CommandText);
            StringAssert.Contains(command.CommandText, "ORDER BY");
        }

        [TestMethod]
        public void Handle_Adds_Offset_And_PageSize_Parameters()
        {
            var handler = CreateHandler();
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardMessagesQuery(2, 50, null), command, CommandStringTypes.GetDashboardMessages);

            var parameters = (DataParameterCollection)command.Parameters;
            Assert.AreEqual(2, parameters.Count);
            Assert.IsTrue(parameters.Any(p => p.ParameterName == "@Offset"));
            Assert.IsTrue(parameters.Any(p => p.ParameterName == "@PageSize"));
        }

        [TestMethod]
        public void Handle_Adds_Status_Parameter_When_Filter_Set()
        {
            var handler = CreateHandler();
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardMessagesQuery(0, 25, 1), command, CommandStringTypes.GetDashboardMessages);

            var parameters = (DataParameterCollection)command.Parameters;
            Assert.AreEqual(3, parameters.Count);
            Assert.IsTrue(parameters.Any(p => p.ParameterName == "@Status"));
            StringAssert.Contains(command.CommandText, "WHERE Status = @Status");
        }

        [TestMethod]
        public void Handle_Computes_Offset_From_PageIndex()
        {
            var handler = CreateHandler();
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardMessagesQuery(3, 10, null), command, CommandStringTypes.GetDashboardMessages);

            var parameters = (DataParameterCollection)command.Parameters;
            var offset = parameters.First(p => p.ParameterName == "@Offset");
            Assert.AreEqual(30, offset.Value); // 3 * 10
        }

        private static GetDashboardMessagesPrepareHandler CreateHandler()
        {
            var cache = new FakeCommandStringCache();
            var optionsFactory = Substitute.For<ITransportOptionsFactory>();
            var options = Substitute.For<ITransportOptions>();
            optionsFactory.Create().Returns(options);
            return new GetDashboardMessagesPrepareHandler(cache, optionsFactory);
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
