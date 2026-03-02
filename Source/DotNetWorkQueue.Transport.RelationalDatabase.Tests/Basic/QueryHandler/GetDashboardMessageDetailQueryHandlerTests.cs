using System;
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryHandler
{
    public class GetDashboardMessageDetailQueryHandlerTests
    {
        [Fact]
        public void Handle_Returns_Message_When_Found()
        {
            var (handler, readColumn, reader) = CreateHandler(true);
            var now = DateTimeOffset.UtcNow;

            readColumn.ReadAsInt64(CommandStringTypes.GetDashboardMessageDetail, 0, reader).Returns(42L);
            readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardMessageDetail, 1, reader).Returns(now);
            readColumn.ReadAsString(CommandStringTypes.GetDashboardMessageDetail, 2, reader).Returns("corr-42");

            var result = handler.Handle(new GetDashboardMessageDetailQuery("42"));

            Assert.NotNull(result);
            Assert.Equal("42", result.QueueId);
            Assert.Equal("corr-42", result.CorrelationId);
        }

        [Fact]
        public void Handle_Returns_Null_When_Not_Found()
        {
            var (handler, _, _) = CreateHandler(false);

            var result = handler.Handle(new GetDashboardMessageDetailQuery("999"));

            Assert.Null(result);
        }

        private static (IQueryHandler<GetDashboardMessageDetailQuery, DashboardMessage> handler, IReadColumn readColumn, IDataReader reader) CreateHandler(bool hasRows)
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardMessageDetailQuery, DashboardMessage>>();
            var readColumn = Substitute.For<IReadColumn>();
            var optionsFactory = Substitute.For<ITransportOptionsFactory>();
            var options = Substitute.For<ITransportOptions>();
            optionsFactory.Create().Returns(options);

            var connection = Substitute.For<IDbConnection>();
            var command = Substitute.For<IDbCommand>();
            var reader = Substitute.For<IDataReader>();
            reader.Read().Returns(hasRows, false);

            factory.Create().Returns(connection);
            connection.CreateCommand().Returns(command);
            command.ExecuteReader().Returns(reader);

            var handler = new GetDashboardMessageDetailQueryHandler(factory, prepareQuery, readColumn, optionsFactory);
            return (handler, readColumn, reader);
        }
    }
}
