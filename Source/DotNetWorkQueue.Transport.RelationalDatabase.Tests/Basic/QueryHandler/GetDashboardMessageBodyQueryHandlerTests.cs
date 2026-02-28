using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler;
using DotNetWorkQueue.Transport.Shared;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryHandler
{
    public class GetDashboardMessageBodyQueryHandlerTests
    {
        [Fact]
        public void Handle_Returns_Body_When_Found()
        {
            var (handler, readColumn, reader) = CreateHandler(true);
            var bodyBytes = new byte[] { 1, 2, 3 };
            var headerBytes = new byte[] { 4, 5, 6 };

            readColumn.ReadAsByteArray(CommandStringTypes.GetDashboardMessageBody, 0, reader).Returns(bodyBytes);
            readColumn.ReadAsByteArray(CommandStringTypes.GetDashboardMessageBody, 1, reader).Returns(headerBytes);

            var result = handler.Handle(new GetDashboardMessageBodyQuery(42));

            Assert.NotNull(result);
            Assert.Equal(bodyBytes, result.Body);
            Assert.Equal(headerBytes, result.Headers);
        }

        [Fact]
        public void Handle_Returns_Null_When_Not_Found()
        {
            var (handler, _, _) = CreateHandler(false);

            var result = handler.Handle(new GetDashboardMessageBodyQuery(999));

            Assert.Null(result);
        }

        private static (IQueryHandler<GetDashboardMessageBodyQuery, DashboardMessageBody> handler, IReadColumn readColumn, IDataReader reader) CreateHandler(bool hasRows)
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardMessageBodyQuery, DashboardMessageBody>>();
            var readColumn = Substitute.For<IReadColumn>();

            var connection = Substitute.For<IDbConnection>();
            var command = Substitute.For<IDbCommand>();
            var reader = Substitute.For<IDataReader>();
            reader.Read().Returns(hasRows, false);

            factory.Create().Returns(connection);
            connection.CreateCommand().Returns(command);
            command.ExecuteReader().Returns(reader);

            var handler = new GetDashboardMessageBodyQueryHandler(factory, prepareQuery, readColumn);
            return (handler, readColumn, reader);
        }
    }
}
