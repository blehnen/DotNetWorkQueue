using System.Data;
using System.Linq;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryPrepareHandler
{
    public class GetDashboardStaleMessagesPrepareHandlerTests
    {
        [Fact]
        public void Handle_Sets_CommandText()
        {
            var handler = CreateHandler();
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardStaleMessagesQuery(60, 0, 25), command, CommandStringTypes.GetDashboardStaleMessages);

            Assert.NotNull(command.CommandText);
        }

        [Fact]
        public void Handle_Adds_Three_Parameters()
        {
            var handler = CreateHandler();
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardStaleMessagesQuery(120, 2, 50), command, CommandStringTypes.GetDashboardStaleMessages);

            var parameters = (DataParameterCollection)command.Parameters;
            Assert.Equal(4, parameters.Count);
            Assert.True(parameters.Any(p => p.ParameterName == "@Threshold"));
            Assert.True(parameters.Any(p => p.ParameterName == "@ThresholdTicks"));
            Assert.True(parameters.Any(p => p.ParameterName == "@Offset"));
            Assert.True(parameters.Any(p => p.ParameterName == "@PageSize"));
        }

        [Fact]
        public void Handle_Computes_Offset_From_PageIndex()
        {
            var handler = CreateHandler();
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardStaleMessagesQuery(60, 3, 10), command, CommandStringTypes.GetDashboardStaleMessages);

            var parameters = (DataParameterCollection)command.Parameters;
            var offset = parameters.First(p => p.ParameterName == "@Offset");
            Assert.Equal(30, offset.Value); // 3 * 10
        }

        private static GetDashboardStaleMessagesPrepareHandler CreateHandler()
        {
            var cache = new FakeCommandStringCache();
            var optionsFactory = Substitute.For<ITransportOptionsFactory>();
            var options = Substitute.For<ITransportOptions>();
            optionsFactory.Create().Returns(options);
            return new GetDashboardStaleMessagesPrepareHandler(cache, optionsFactory);
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
