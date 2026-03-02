using System;
using System.Collections.Generic;
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
    public class GetDashboardStaleMessagesQueryHandlerTests
    {
        [Fact]
        public void Handle_Returns_Messages_From_Reader()
        {
            var (handler, readColumn, reader) = CreateHandler(1);

            readColumn.ReadAsInt64(CommandStringTypes.GetDashboardStaleMessages, 0, reader).Returns(1L);
            readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardStaleMessages, 1, reader).Returns(DateTimeOffset.UtcNow);
            readColumn.ReadAsString(CommandStringTypes.GetDashboardStaleMessages, 2, reader).Returns("corr-1");

            var result = handler.Handle(new GetDashboardStaleMessagesQuery(60, 0, 25));

            Assert.Single(result);
            Assert.Equal("1", result[0].QueueId);
        }

        [Fact]
        public void Handle_Returns_Empty_List_When_No_Rows()
        {
            var (handler, _, _) = CreateHandler(0);

            var result = handler.Handle(new GetDashboardStaleMessagesQuery(60, 0, 25));

            Assert.Empty(result);
        }

        private static (IQueryHandler<GetDashboardStaleMessagesQuery, IReadOnlyList<DashboardMessage>> handler, IReadColumn readColumn, IDataReader reader) CreateHandler(int rowCount)
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardStaleMessagesQuery, IReadOnlyList<DashboardMessage>>>();
            var readColumn = Substitute.For<IReadColumn>();
            var optionsFactory = Substitute.For<ITransportOptionsFactory>();
            var options = Substitute.For<ITransportOptions>();
            optionsFactory.Create().Returns(options);

            var connection = Substitute.For<IDbConnection>();
            var command = Substitute.For<IDbCommand>();
            var reader = Substitute.For<IDataReader>();

            if (rowCount > 0)
                reader.Read().Returns(true, false);
            else
                reader.Read().Returns(false);

            factory.Create().Returns(connection);
            connection.CreateCommand().Returns(command);
            command.ExecuteReader().Returns(reader);

            var handler = new GetDashboardStaleMessagesQueryHandler(factory, prepareQuery, readColumn, optionsFactory);
            return (handler, readColumn, reader);
        }
    }
}
