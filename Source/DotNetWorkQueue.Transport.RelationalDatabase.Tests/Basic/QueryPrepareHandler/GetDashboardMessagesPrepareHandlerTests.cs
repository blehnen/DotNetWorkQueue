using System.Data;
using System.Linq;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryPrepareHandler
{
    public class GetDashboardMessagesPrepareHandlerTests
    {
        [Fact]
        public void Handle_Sets_CommandText()
        {
            var handler = CreateHandler();
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardMessagesQuery(0, 25, null), command, CommandStringTypes.GetDashboardMessages);

            Assert.NotNull(command.CommandText);
            Assert.Contains("ORDER BY", command.CommandText);
        }

        [Fact]
        public void Handle_Adds_Offset_And_PageSize_Parameters()
        {
            var handler = CreateHandler();
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardMessagesQuery(2, 50, null), command, CommandStringTypes.GetDashboardMessages);

            var parameters = (DataParameterCollection)command.Parameters;
            Assert.Equal(2, parameters.Count);
            Assert.True(parameters.Any(p => p.ParameterName == "@Offset"));
            Assert.True(parameters.Any(p => p.ParameterName == "@PageSize"));
        }

        [Fact]
        public void Handle_Adds_Status_Parameter_When_Filter_Set()
        {
            var handler = CreateHandler();
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardMessagesQuery(0, 25, 1), command, CommandStringTypes.GetDashboardMessages);

            var parameters = (DataParameterCollection)command.Parameters;
            Assert.Equal(3, parameters.Count);
            Assert.True(parameters.Any(p => p.ParameterName == "@Status"));
            Assert.Contains("WHERE Status = @Status", command.CommandText);
        }

        [Fact]
        public void Handle_Computes_Offset_From_PageIndex()
        {
            var handler = CreateHandler();
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardMessagesQuery(3, 10, null), command, CommandStringTypes.GetDashboardMessages);

            var parameters = (DataParameterCollection)command.Parameters;
            var offset = parameters.First(p => p.ParameterName == "@Offset");
            Assert.Equal(30, offset.Value); // 3 * 10
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
