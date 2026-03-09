using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
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
    public class GetDashboardStatusCountsQueryHandlerTests
    {
        [TestMethod]
        public void Handle_Returns_Counts_From_Reader()
        {
            var (handler, readColumn, reader) = CreateHandler(true);
            readColumn.ReadAsInt64(CommandStringTypes.GetDashboardStatusCounts, 0, reader).Returns(10L);
            readColumn.ReadAsInt64(CommandStringTypes.GetDashboardStatusCounts, 1, reader).Returns(5L);
            readColumn.ReadAsInt64(CommandStringTypes.GetDashboardStatusCounts, 2, reader).Returns(2L);
            readColumn.ReadAsInt64(CommandStringTypes.GetDashboardStatusCounts, 3, reader).Returns(17L);

            var result = handler.Handle(new GetDashboardStatusCountsQuery());

            Assert.AreEqual(10L, result.Waiting);
            Assert.AreEqual(5L, result.Processing);
            Assert.AreEqual(2L, result.Error);
            Assert.AreEqual(17L, result.Total);
        }

        [TestMethod]
        public void Handle_Returns_Default_When_No_Rows()
        {
            var (handler, _, _) = CreateHandler(false);

            var result = handler.Handle(new GetDashboardStatusCountsQuery());

            Assert.AreEqual(0L, result.Waiting);
            Assert.AreEqual(0L, result.Processing);
            Assert.AreEqual(0L, result.Error);
            Assert.AreEqual(0L, result.Total);
        }

        private static (IQueryHandler<GetDashboardStatusCountsQuery, DashboardStatusCounts> handler, IReadColumn readColumn, IDataReader reader) CreateHandler(bool hasRows)
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardStatusCountsQuery, DashboardStatusCounts>>();
            var readColumn = Substitute.For<IReadColumn>();

            var connection = Substitute.For<IDbConnection>();
            var command = Substitute.For<IDbCommand>();
            var reader = Substitute.For<IDataReader>();
            reader.Read().Returns(hasRows, false);

            factory.Create().Returns(connection);
            connection.CreateCommand().Returns(command);
            command.ExecuteReader().Returns(reader);

            var handler = new GetDashboardStatusCountsQueryHandler(factory, prepareQuery, readColumn);
            return (handler, readColumn, reader);
        }
    }
}
