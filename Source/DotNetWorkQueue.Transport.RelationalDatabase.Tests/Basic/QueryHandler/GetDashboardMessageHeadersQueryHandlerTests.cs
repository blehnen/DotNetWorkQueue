using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryHandler
{
    [TestClass]
    public class GetDashboardMessageHeadersQueryHandlerTests
    {
        [TestMethod]
        public void Handle_Returns_Headers_When_Found()
        {
            var (handler, readColumn, reader) = CreateHandler(true);
            var headerBytes = new byte[] { 7, 8, 9 };

            readColumn.ReadAsByteArray(CommandStringTypes.GetDashboardMessageHeaders, 0, reader).Returns(headerBytes);

            var result = handler.Handle(new GetDashboardMessageHeadersQuery("42"));

            Assert.IsNotNull(result);
            Assert.AreEqual(headerBytes, result.Headers);
        }

        [TestMethod]
        public void Handle_Returns_Null_When_Not_Found()
        {
            var (handler, _, _) = CreateHandler(false);

            var result = handler.Handle(new GetDashboardMessageHeadersQuery("999"));

            Assert.IsNull(result);
        }

        private static (IQueryHandler<GetDashboardMessageHeadersQuery, DashboardMessageHeaders> handler, IReadColumn readColumn, IDataReader reader) CreateHandler(bool hasRows)
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardMessageHeadersQuery, DashboardMessageHeaders>>();
            var readColumn = Substitute.For<IReadColumn>();

            var connection = Substitute.For<IDbConnection>();
            var command = Substitute.For<IDbCommand>();
            var reader = Substitute.For<IDataReader>();
            reader.Read().Returns(hasRows, false);

            factory.Create().Returns(connection);
            connection.CreateCommand().Returns(command);
            command.ExecuteReader().Returns(reader);

            var handler = new GetDashboardMessageHeadersQueryHandler(factory, prepareQuery, readColumn);
            return (handler, readColumn, reader);
        }
    }
}
