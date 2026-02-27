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
    public class GetDashboardConfigurationQueryHandlerTests
    {
        [Fact]
        public void Handle_Returns_Bytes_From_Reader()
        {
            var (handler, readColumn, reader) = CreateHandler(true);
            var expected = System.Text.Encoding.UTF8.GetBytes("{\"test\":true}");
            readColumn.ReadAsByteArray(CommandStringTypes.GetDashboardConfiguration, 0, reader).Returns(expected);

            var result = handler.Handle(new GetDashboardConfigurationQuery());

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Handle_Returns_Null_When_No_Rows()
        {
            var (handler, _, _) = CreateHandler(false);

            var result = handler.Handle(new GetDashboardConfigurationQuery());

            Assert.Null(result);
        }

        private static (IQueryHandler<GetDashboardConfigurationQuery, byte[]> handler, IReadColumn readColumn, IDataReader reader) CreateHandler(bool hasRows)
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardConfigurationQuery, byte[]>>();
            var readColumn = Substitute.For<IReadColumn>();

            var connection = Substitute.For<IDbConnection>();
            var command = Substitute.For<IDbCommand>();
            var reader = Substitute.For<IDataReader>();
            reader.Read().Returns(hasRows, false);

            factory.Create().Returns(connection);
            connection.CreateCommand().Returns(command);
            command.ExecuteReader().Returns(reader);

            var handler = new GetDashboardConfigurationQueryHandler(factory, prepareQuery, readColumn);
            return (handler, readColumn, reader);
        }
    }
}
