using System.Data;
using System.Linq;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryPrepareHandler
{
    public class GetDashboardMessageCountPrepareHandlerTests
    {
        [Fact]
        public void Handle_Sets_CommandText_Without_Filter()
        {
            var cache = new FakeCommandStringCache();
            var handler = new GetDashboardMessageCountPrepareHandler(cache);
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardMessageCountQuery(null), command, CommandStringTypes.GetDashboardMessageCount);

            Assert.Equal("SELECT COUNT(*) FROM meta", command.CommandText);
        }

        [Fact]
        public void Handle_No_Parameters_Without_Filter()
        {
            var cache = new FakeCommandStringCache();
            var handler = new GetDashboardMessageCountPrepareHandler(cache);
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardMessageCountQuery(null), command, CommandStringTypes.GetDashboardMessageCount);

            Assert.Empty(((DataParameterCollection)command.Parameters));
        }

        [Fact]
        public void Handle_Adds_Status_Parameter_When_Filter_Set()
        {
            var cache = new FakeCommandStringCache();
            var handler = new GetDashboardMessageCountPrepareHandler(cache);
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardMessageCountQuery(1), command, CommandStringTypes.GetDashboardMessageCount);

            var parameters = (DataParameterCollection)command.Parameters;
            Assert.Empty(parameters);
            Assert.True(parameters.Any(p => p.ParameterName == "@Status"));
            Assert.Contains("WHERE Status = @Status", command.CommandText);
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
