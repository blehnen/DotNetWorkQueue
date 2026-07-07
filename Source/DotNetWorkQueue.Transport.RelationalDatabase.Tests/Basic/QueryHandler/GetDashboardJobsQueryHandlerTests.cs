using System;
using System.Collections.Generic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler;
using DotNetWorkQueue.Transport.RelationalDatabase.Tests.TestHelpers;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryHandler
{
    [TestClass]
    public class GetDashboardJobsQueryHandlerTests
    {
        [TestMethod]
        public void Handle_Returns_Jobs_From_Reader()
        {
            var (handler, fixture, _) = CreateHandler(1);
            var now = DateTimeOffset.UtcNow;

            fixture.ReadColumn.ReadAsString(CommandStringTypes.GetDashboardJobs, 0, fixture.Reader).Returns("TestJob");
            fixture.ReadColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardJobs, 1, fixture.Reader).Returns(now);
            fixture.ReadColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardJobs, 2, fixture.Reader).Returns(now.AddMinutes(5));

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
            var (handler, fixture, _) = CreateHandler(3);
            var now = DateTimeOffset.UtcNow;

            fixture.ReadColumn.ReadAsString(CommandStringTypes.GetDashboardJobs, 0, fixture.Reader).Returns("JobA", "JobB", "JobC");
            fixture.ReadColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardJobs, 1, fixture.Reader).Returns(now, now.AddMinutes(1), now.AddMinutes(2));
            fixture.ReadColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardJobs, 2, fixture.Reader).Returns(now.AddMinutes(5), now.AddMinutes(6), now.AddMinutes(7));

            var result = handler.Handle(new GetDashboardJobsQuery());

            Assert.HasCount(3, result);
            Assert.AreEqual("JobA", result[0].JobName);
            Assert.AreEqual("JobB", result[1].JobName);
            Assert.AreEqual("JobC", result[2].JobName);
            fixture.ReadColumn.Received(3).ReadAsString(CommandStringTypes.GetDashboardJobs, 0, fixture.Reader);
            fixture.ReadColumn.Received(3).ReadAsDateTimeOffset(CommandStringTypes.GetDashboardJobs, 1, fixture.Reader);
            fixture.ReadColumn.Received(3).ReadAsDateTimeOffset(CommandStringTypes.GetDashboardJobs, 2, fixture.Reader);
        }

        [TestMethod]
        public void Handle_Invokes_PrepareQuery_With_Correct_CommandString()
        {
            var (handler, fixture, prepareQuery) = CreateHandler(0);
            var query = new GetDashboardJobsQuery();

            handler.Handle(query);

            prepareQuery.Received(1).Handle(query, fixture.Command, CommandStringTypes.GetDashboardJobs);
            fixture.Connection.Received(1).Open();
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

        private static (GetDashboardJobsQueryHandler handler,
                        AdoNetMockFixture fixture,
                        IPrepareQueryHandler<GetDashboardJobsQuery, IReadOnlyList<DashboardJob>> prepareQuery) CreateHandler(int rowCount)
        {
            var fixture = AdoNetMockFixture.Create();
            fixture.SetupReaderRows(rowCount);
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardJobsQuery, IReadOnlyList<DashboardJob>>>();
            var handler = new GetDashboardJobsQueryHandler(fixture.ConnectionFactory, prepareQuery, fixture.ReadColumn);
            return (handler, fixture, prepareQuery);
        }
    }
}
