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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryHandler
{
    [TestClass]
    public class GetDashboardJobsQueryHandlerTests
    {
        [TestMethod]
        public void Handle_Returns_Jobs_From_Reader()
        {
            var (handler, readColumn, reader) = CreateHandler(1);
            var now = DateTimeOffset.UtcNow;

            readColumn.ReadAsString(CommandStringTypes.GetDashboardJobs, 0, reader).Returns("TestJob");
            readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardJobs, 1, reader).Returns(now);
            readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardJobs, 2, reader).Returns(now.AddMinutes(5));

            var result = handler.Handle(new GetDashboardJobsQuery());

            Assert.ContainsSingle(result);
            Assert.AreEqual("TestJob", result[0].JobName);
            Assert.AreEqual(now, result[0].JobEventTime);
        }

        [TestMethod]
        public void Handle_Returns_Empty_List_When_No_Rows()
        {
            var (handler, _, _) = CreateHandler(0);

            var result = handler.Handle(new GetDashboardJobsQuery());

            Assert.IsEmpty(result);
        }

        private static (IQueryHandler<GetDashboardJobsQuery, IReadOnlyList<DashboardJob>> handler, IReadColumn readColumn, IDataReader reader) CreateHandler(int rowCount)
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardJobsQuery, IReadOnlyList<DashboardJob>>>();
            var readColumn = Substitute.For<IReadColumn>();

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

            var handler = new GetDashboardJobsQueryHandler(factory, prepareQuery, readColumn);
            return (handler, readColumn, reader);
        }
    }
}
