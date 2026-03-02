using System.Data;
using System.Linq;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryPrepareHandler
{
    public class GetDashboardErrorMessagesPrepareHandlerTests
    {
        [Fact]
        public void Handle_Sets_CommandText()
        {
            var cache = new FakeCommandStringCache();
            var handler = new GetDashboardErrorMessagesPrepareHandler(cache);
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardErrorMessagesQuery(0, 25), command, CommandStringTypes.GetDashboardErrorMessages);

            Assert.Equal("SELECT errors", command.CommandText);
        }

        [Fact]
        public void Handle_Adds_Offset_And_PageSize_Parameters()
        {
            var cache = new FakeCommandStringCache();
            var handler = new GetDashboardErrorMessagesPrepareHandler(cache);
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardErrorMessagesQuery(1, 50), command, CommandStringTypes.GetDashboardErrorMessages);

            var parameters = (DataParameterCollection)command.Parameters;
            Assert.Equal(2, parameters.Count);
            Assert.True(parameters.Any(p => p.ParameterName == "@Offset"));
            Assert.True(parameters.Any(p => p.ParameterName == "@PageSize"));
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
