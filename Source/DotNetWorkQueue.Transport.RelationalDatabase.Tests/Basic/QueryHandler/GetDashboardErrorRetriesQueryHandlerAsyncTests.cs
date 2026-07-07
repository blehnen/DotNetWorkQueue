// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    public class GetDashboardErrorRetriesQueryHandlerAsyncTests
    {
        [TestMethod]
        public async Task HandleAsync_Returns_Retries()
        {
            var (handler, fixture, _) = CreateHandler(1);

            fixture.ReadColumn.ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 0, fixture.Reader).Returns(10L);
            fixture.ReadColumn.ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 1, fixture.Reader).Returns(42L);
            fixture.ReadColumn.ReadAsString(CommandStringTypes.GetDashboardErrorRetries, 2, fixture.Reader).Returns("TimeoutException");
            fixture.ReadColumn.ReadAsInt32(CommandStringTypes.GetDashboardErrorRetries, 3, fixture.Reader).Returns(3);

            var result = await handler.HandleAsync(new GetDashboardErrorRetriesQuery("42"));

            Assert.ContainsSingle(result);
            Assert.AreEqual(10L, result[0].ErrorTrackingId);
            Assert.AreEqual("42", result[0].QueueId);
            Assert.AreEqual("TimeoutException", result[0].ExceptionType);
            Assert.AreEqual(3, result[0].RetryCount);
        }

        [TestMethod]
        public async Task HandleAsync_Returns_Empty_When_No_Rows()
        {
            var (handler, _, _) = CreateHandler(0);

            var result = await handler.HandleAsync(new GetDashboardErrorRetriesQuery("42"));

            Assert.IsEmpty(result);
        }

        [TestMethod]
        public async Task HandleAsync_Returns_Multiple_Retries_From_Reader()
        {
            var (handler, fixture, _) = CreateHandler(3);

            fixture.ReadColumn.ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 0, fixture.Reader).Returns(10L, 20L, 30L);
            fixture.ReadColumn.ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 1, fixture.Reader).Returns(100L, 200L, 300L);
            fixture.ReadColumn.ReadAsString(CommandStringTypes.GetDashboardErrorRetries, 2, fixture.Reader).Returns("ExA", "ExB", "ExC");
            fixture.ReadColumn.ReadAsInt32(CommandStringTypes.GetDashboardErrorRetries, 3, fixture.Reader).Returns(1, 2, 3);

            var result = await handler.HandleAsync(new GetDashboardErrorRetriesQuery("100"));

            Assert.HasCount(3, result);
            Assert.AreEqual(10L, result[0].ErrorTrackingId);
            Assert.AreEqual("100", result[0].QueueId);
            Assert.AreEqual(30L, result[2].ErrorTrackingId);
            Assert.AreEqual("300", result[2].QueueId);
            fixture.ReadColumn.Received(3).ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 0, fixture.Reader);
            fixture.ReadColumn.Received(3).ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 1, fixture.Reader);
            fixture.ReadColumn.Received(3).ReadAsString(CommandStringTypes.GetDashboardErrorRetries, 2, fixture.Reader);
            fixture.ReadColumn.Received(3).ReadAsInt32(CommandStringTypes.GetDashboardErrorRetries, 3, fixture.Reader);
        }

        [TestMethod]
        public async Task HandleAsync_Awaited_Result_Matches_Mocked_Reader_Output()
        {
            var (handler, fixture, _) = CreateHandler(1);

            fixture.ReadColumn.ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 0, fixture.Reader).Returns(99L);
            fixture.ReadColumn.ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 1, fixture.Reader).Returns(777L);
            fixture.ReadColumn.ReadAsString(CommandStringTypes.GetDashboardErrorRetries, 2, fixture.Reader).Returns("SpecificException");
            fixture.ReadColumn.ReadAsInt32(CommandStringTypes.GetDashboardErrorRetries, 3, fixture.Reader).Returns(5);

            var task = handler.HandleAsync(new GetDashboardErrorRetriesQuery("777"));
            var awaited = await task;

            Assert.IsTrue(task.IsCompletedSuccessfully);
            Assert.ContainsSingle(awaited);
            Assert.AreEqual(99L, awaited[0].ErrorTrackingId);
            Assert.AreEqual("777", awaited[0].QueueId);
            Assert.AreEqual("SpecificException", awaited[0].ExceptionType);
            Assert.AreEqual(5, awaited[0].RetryCount);
        }

        [TestMethod]
        public async Task HandleAsync_Invokes_PrepareQuery_With_Correct_CommandString()
        {
            var (handler, fixture, prepareQuery) = CreateHandler(0);
            var query = new GetDashboardErrorRetriesQuery("42");

            await handler.HandleAsync(query);

            prepareQuery.Received(1).Handle(query, fixture.Command, CommandStringTypes.GetDashboardErrorRetries);
        }

        [TestMethod]
        public void Constructor_Throws_When_DbConnectionFactory_Is_Null()
        {
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardErrorRetriesQuery, IReadOnlyList<DashboardErrorRetry>>>();
            var readColumn = Substitute.For<IReadColumn>();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new GetDashboardErrorRetriesQueryHandlerAsync(null, prepareQuery, readColumn));
        }

        [TestMethod]
        public void Constructor_Throws_When_PrepareQuery_Is_Null()
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var readColumn = Substitute.For<IReadColumn>();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new GetDashboardErrorRetriesQueryHandlerAsync(factory, null, readColumn));
        }

        [TestMethod]
        public void Constructor_Throws_When_ReadColumn_Is_Null()
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardErrorRetriesQuery, IReadOnlyList<DashboardErrorRetry>>>();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new GetDashboardErrorRetriesQueryHandlerAsync(factory, prepareQuery, null));
        }

        private static (GetDashboardErrorRetriesQueryHandlerAsync handler,
                        AdoNetAsyncMockFixture fixture,
                        IPrepareQueryHandler<GetDashboardErrorRetriesQuery, IReadOnlyList<DashboardErrorRetry>> prepareQuery) CreateHandler(int rowCount)
        {
            var fixture = AdoNetAsyncMockFixture.Create();
            fixture.SetupReaderRows(rowCount);
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardErrorRetriesQuery, IReadOnlyList<DashboardErrorRetry>>>();
            var handler = new GetDashboardErrorRetriesQueryHandlerAsync(fixture.ConnectionFactory, prepareQuery, fixture.ReadColumn);
            return (handler, fixture, prepareQuery);
        }
    }
}
