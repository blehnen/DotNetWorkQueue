using System;
using System.Collections.Generic;
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler;
using DotNetWorkQueue.Transport.Shared;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryHandler
{
    public class GetDashboardMessagesQueryHandlerTests
    {
        [Fact]
        public void Handle_Returns_Messages_From_Reader()
        {
            var (handler, readColumn, reader) = CreateHandler(2);
            var now = DateTimeOffset.UtcNow;

            readColumn.ReadAsInt64(CommandStringTypes.GetDashboardMessages, 0, reader).Returns(1L, 2L);
            readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardMessages, 1, reader).Returns(now);
            readColumn.ReadAsString(CommandStringTypes.GetDashboardMessages, 2, reader).Returns("corr-1", "corr-2");

            var result = handler.Handle(new GetDashboardMessagesQuery(0, 25, null));

            Assert.Equal(2, result.Count);
            Assert.Equal(1L, result[0].QueueId);
            Assert.Equal("corr-1", result[0].CorrelationId);
        }

        [Fact]
        public void Handle_Returns_Empty_List_When_No_Rows()
        {
            var (handler, _, _) = CreateHandler(0);

            var result = handler.Handle(new GetDashboardMessagesQuery(0, 25, null));

            Assert.Empty(result);
        }

        private static (IQueryHandler<GetDashboardMessagesQuery, IReadOnlyList<DashboardMessage>> handler, IReadColumn readColumn, IDataReader reader) CreateHandler(int rowCount)
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardMessagesQuery, IReadOnlyList<DashboardMessage>>>();
            var readColumn = Substitute.For<IReadColumn>();
            var optionsFactory = Substitute.For<ITransportOptionsFactory>();
            var options = Substitute.For<ITransportOptions>();
            optionsFactory.Create().Returns(options);

            var connection = Substitute.For<IDbConnection>();
            var command = Substitute.For<IDbCommand>();
            var reader = Substitute.For<IDataReader>();

            if (rowCount == 0)
                reader.Read().Returns(false);
            else if (rowCount == 1)
                reader.Read().Returns(true, false);
            else
                reader.Read().Returns(true, true, false);

            factory.Create().Returns(connection);
            connection.CreateCommand().Returns(command);
            command.ExecuteReader().Returns(reader);

            var handler = new GetDashboardMessagesQueryHandler(factory, prepareQuery, readColumn, optionsFactory);
            return (handler, readColumn, reader);
        }
    }
}
