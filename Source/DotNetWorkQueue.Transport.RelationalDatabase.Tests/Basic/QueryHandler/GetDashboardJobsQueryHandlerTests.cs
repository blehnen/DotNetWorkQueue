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

        [TestMethod]
        public void Handle_Returns_Multiple_Jobs_From_Reader()
        {
            var (handler, readColumn, reader) = CreateHandler(3);
            var now = DateTimeOffset.UtcNow;

            readColumn.ReadAsString(CommandStringTypes.GetDashboardJobs, 0, reader).Returns("JobA", "JobB", "JobC");
            readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardJobs, 1, reader).Returns(now, now.AddMinutes(1), now.AddMinutes(2));
            readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardJobs, 2, reader).Returns(now.AddMinutes(5), now.AddMinutes(6), now.AddMinutes(7));

            var result = handler.Handle(new GetDashboardJobsQuery());

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("JobA", result[0].JobName);
            Assert.AreEqual("JobB", result[1].JobName);
            Assert.AreEqual("JobC", result[2].JobName);
            readColumn.Received(3).ReadAsString(CommandStringTypes.GetDashboardJobs, 0, reader);
            readColumn.Received(3).ReadAsDateTimeOffset(CommandStringTypes.GetDashboardJobs, 1, reader);
            readColumn.Received(3).ReadAsDateTimeOffset(CommandStringTypes.GetDashboardJobs, 2, reader);
        }

        [TestMethod]
        public void Handle_Invokes_PrepareQuery_With_Correct_CommandString()
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardJobsQuery, IReadOnlyList<DashboardJob>>>();
            var readColumn = Substitute.For<IReadColumn>();

            var connection = Substitute.For<IDbConnection>();
            var command = Substitute.For<IDbCommand>();
            var reader = Substitute.For<IDataReader>();
            reader.Read().Returns(false);

            factory.Create().Returns(connection);
            connection.CreateCommand().Returns(command);
            command.ExecuteReader().Returns(reader);

            var handler = new GetDashboardJobsQueryHandler(factory, prepareQuery, readColumn);
            var query = new GetDashboardJobsQuery();

            handler.Handle(query);

            prepareQuery.Received(1).Handle(query, command, CommandStringTypes.GetDashboardJobs);
            connection.Received(1).Open();
        }

        [TestMethod]
        public void Constructor_Throws_When_DbConnectionFactory_Is_Null()
        {
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardJobsQuery, IReadOnlyList<DashboardJob>>>();
            var readColumn = Substitute.For<IReadColumn>();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new GetDashboardJobsQueryHandler(null, prepareQuery, readColumn));
        }

        [TestMethod]
        public void Constructor_Throws_When_PrepareQuery_Is_Null()
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var readColumn = Substitute.For<IReadColumn>();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new GetDashboardJobsQueryHandler(factory, null, readColumn));
        }

        [TestMethod]
        public void Constructor_Throws_When_ReadColumn_Is_Null()
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardJobsQuery, IReadOnlyList<DashboardJob>>>();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new GetDashboardJobsQueryHandler(factory, prepareQuery, null));
        }

        private static (IQueryHandler<GetDashboardJobsQuery, IReadOnlyList<DashboardJob>> handler, IReadColumn readColumn, IDataReader reader) CreateHandler(int rowCount)
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardJobsQuery, IReadOnlyList<DashboardJob>>>();
            var readColumn = Substitute.For<IReadColumn>();

            var connection = Substitute.For<IDbConnection>();
            var command = Substitute.For<IDbCommand>();
            var reader = Substitute.For<IDataReader>();

            if (rowCount <= 0)
            {
                reader.Read().Returns(false);
            }
            else
            {
                var rest = new bool[rowCount];
                for (var i = 0; i < rowCount - 1; i++) rest[i] = true;
                rest[rowCount - 1] = false;
                reader.Read().Returns(true, rest);
            }

            factory.Create().Returns(connection);
            connection.CreateCommand().Returns(command);
            command.ExecuteReader().Returns(reader);

            var handler = new GetDashboardJobsQueryHandler(factory, prepareQuery, readColumn);
            return (handler, readColumn, reader);
        }
    }
}
