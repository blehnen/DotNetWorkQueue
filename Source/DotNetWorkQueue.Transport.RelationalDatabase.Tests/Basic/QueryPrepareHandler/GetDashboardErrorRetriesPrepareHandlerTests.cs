using System.Data;
using System.Linq;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryPrepareHandler
{
    public class GetDashboardErrorRetriesPrepareHandlerTests
    {
        [Fact]
        public void Handle_Sets_CommandText()
        {
            var cache = new FakeCommandStringCache();
            var handler = new GetDashboardErrorRetriesPrepareHandler(cache);
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardErrorRetriesQuery(42), command, CommandStringTypes.GetDashboardErrorRetries);

            Assert.Equal("SELECT retries", command.CommandText);
        }

        [Fact]
        public void Handle_Adds_QueueId_Parameter()
        {
            var cache = new FakeCommandStringCache();
            var handler = new GetDashboardErrorRetriesPrepareHandler(cache);
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardErrorRetriesQuery(42), command, CommandStringTypes.GetDashboardErrorRetries);

            var parameters = (DataParameterCollection)command.Parameters;
            Assert.Empty(parameters);
            var param = parameters.First();
            Assert.Equal("@QueueId", param.ParameterName);
            Assert.Equal(42L, param.Value);
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
