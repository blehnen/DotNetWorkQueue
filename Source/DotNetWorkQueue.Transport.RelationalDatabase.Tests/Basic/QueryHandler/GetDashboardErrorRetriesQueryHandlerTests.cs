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
    public class GetDashboardErrorRetriesQueryHandlerTests
    {
        [TestMethod]
        public void Handle_Returns_Retries_From_Reader()
        {
            var (handler, fixture, _) = CreateHandler(1);

            fixture.ReadColumn.ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 0, fixture.Reader).Returns(10L);
            fixture.ReadColumn.ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 1, fixture.Reader).Returns(42L);
            fixture.ReadColumn.ReadAsString(CommandStringTypes.GetDashboardErrorRetries, 2, fixture.Reader).Returns("TimeoutException");
            fixture.ReadColumn.ReadAsInt32(CommandStringTypes.GetDashboardErrorRetries, 3, fixture.Reader).Returns(3);

            var result = handler.Handle(new GetDashboardErrorRetriesQuery("42"));

            Assert.ContainsSingle(result);
            Assert.AreEqual(10L, result[0].ErrorTrackingId);
            Assert.AreEqual("42", result[0].QueueId);
            Assert.AreEqual("TimeoutException", result[0].ExceptionType);
            Assert.AreEqual(3, result[0].RetryCount);
        }

        [TestMethod]
        public void Handle_Returns_Empty_List_When_No_Rows()
        {
            var (handler, _, _) = CreateHandler(0);

            var result = handler.Handle(new GetDashboardErrorRetriesQuery("42"));

            Assert.IsEmpty(result);
        }

        [TestMethod]
        public void Handle_Returns_Multiple_Retries_From_Reader()
        {
            var (handler, fixture, _) = CreateHandler(3);

            fixture.ReadColumn.ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 0, fixture.Reader).Returns(10L, 20L, 30L);
            fixture.ReadColumn.ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 1, fixture.Reader).Returns(100L, 200L, 300L);
            fixture.ReadColumn.ReadAsString(CommandStringTypes.GetDashboardErrorRetries, 2, fixture.Reader).Returns("ExA", "ExB", "ExC");
            fixture.ReadColumn.ReadAsInt32(CommandStringTypes.GetDashboardErrorRetries, 3, fixture.Reader).Returns(1, 2, 3);

            var result = handler.Handle(new GetDashboardErrorRetriesQuery("100"));

            Assert.HasCount(3, result);
            Assert.AreEqual(10L, result[0].ErrorTrackingId);
            Assert.AreEqual("100", result[0].QueueId);
            Assert.AreEqual("ExA", result[0].ExceptionType);
            Assert.AreEqual(1, result[0].RetryCount);
            Assert.AreEqual(30L, result[2].ErrorTrackingId);
            Assert.AreEqual("300", result[2].QueueId);
            Assert.AreEqual("ExC", result[2].ExceptionType);
            Assert.AreEqual(3, result[2].RetryCount);
            fixture.ReadColumn.Received(3).ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 0, fixture.Reader);
            fixture.ReadColumn.Received(3).ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 1, fixture.Reader);
            fixture.ReadColumn.Received(3).ReadAsString(CommandStringTypes.GetDashboardErrorRetries, 2, fixture.Reader);
            fixture.ReadColumn.Received(3).ReadAsInt32(CommandStringTypes.GetDashboardErrorRetries, 3, fixture.Reader);
        }

        [TestMethod]
        public void Handle_Invokes_PrepareQuery_With_Correct_CommandString()
        {
            var (handler, fixture, prepareQuery) = CreateHandler(0);
            var query = new GetDashboardErrorRetriesQuery("42");

            handler.Handle(query);

            prepareQuery.Received(1).Handle(query, fixture.Command, CommandStringTypes.GetDashboardErrorRetries);
            fixture.Connection.Received(1).Open();
        }

        [TestMethod]
        public void Constructor_Throws_When_DbConnectionFactory_Is_Null()
        {
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardErrorRetriesQuery, IReadOnlyList<DashboardErrorRetry>>>();
            var readColumn = Substitute.For<IReadColumn>();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new GetDashboardErrorRetriesQueryHandler(null, prepareQuery, readColumn));
        }

        [TestMethod]
        public void Constructor_Throws_When_PrepareQuery_Is_Null()
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var readColumn = Substitute.For<IReadColumn>();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new GetDashboardErrorRetriesQueryHandler(factory, null, readColumn));
        }

        [TestMethod]
        public void Constructor_Throws_When_ReadColumn_Is_Null()
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardErrorRetriesQuery, IReadOnlyList<DashboardErrorRetry>>>();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new GetDashboardErrorRetriesQueryHandler(factory, prepareQuery, null));
        }

        private static (GetDashboardErrorRetriesQueryHandler handler,
                        AdoNetMockFixture fixture,
                        IPrepareQueryHandler<GetDashboardErrorRetriesQuery, IReadOnlyList<DashboardErrorRetry>> prepareQuery) CreateHandler(int rowCount)
        {
            var fixture = AdoNetMockFixture.Create();
            fixture.SetupReaderRows(rowCount);
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardErrorRetriesQuery, IReadOnlyList<DashboardErrorRetry>>>();
            var handler = new GetDashboardErrorRetriesQueryHandler(fixture.ConnectionFactory, prepareQuery, fixture.ReadColumn);
            return (handler, fixture, prepareQuery);
        }
    }
}
