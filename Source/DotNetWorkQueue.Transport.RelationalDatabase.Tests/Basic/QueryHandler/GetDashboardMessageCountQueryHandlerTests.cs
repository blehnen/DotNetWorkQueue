using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryHandler
{
    public class GetDashboardMessageCountQueryHandlerTests
    {
        [Fact]
        public void Handle_Returns_Count_From_Reader()
        {
            var (handler, readColumn, reader) = CreateHandler(true);
            readColumn.ReadAsInt64(CommandStringTypes.GetDashboardMessageCount, 0, reader).Returns(42L);

            var result = handler.Handle(new GetDashboardMessageCountQuery(null));

            Assert.Equal(42L, result);
        }

        [Fact]
        public void Handle_Returns_Zero_When_No_Rows()
        {
            var (handler, _, _) = CreateHandler(false);

            var result = handler.Handle(new GetDashboardMessageCountQuery(null));

            Assert.Equal(0L, result);
        }

        private static (IQueryHandler<GetDashboardMessageCountQuery, long> handler, IReadColumn readColumn, IDataReader reader) CreateHandler(bool hasRows)
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardMessageCountQuery, long>>();
            var readColumn = Substitute.For<IReadColumn>();

            var connection = Substitute.For<IDbConnection>();
            var command = Substitute.For<IDbCommand>();
            var reader = Substitute.For<IDataReader>();
            reader.Read().Returns(hasRows, false);

            factory.Create().Returns(connection);
            connection.CreateCommand().Returns(command);
            command.ExecuteReader().Returns(reader);

            var handler = new GetDashboardMessageCountQueryHandler(factory, prepareQuery, readColumn);
            return (handler, readColumn, reader);
        }
    }
}
