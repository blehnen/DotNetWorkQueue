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
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
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
    public class GetDashboardErrorRetriesQueryHandlerAsyncTests
    {
        [TestMethod]
        public async Task HandleAsync_Returns_Retries()
        {
            var (handler, readColumn, reader) = CreateHandler(1);

            readColumn.ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 0, reader).Returns(10L);
            readColumn.ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 1, reader).Returns(42L);
            readColumn.ReadAsString(CommandStringTypes.GetDashboardErrorRetries, 2, reader).Returns("TimeoutException");
            readColumn.ReadAsInt32(CommandStringTypes.GetDashboardErrorRetries, 3, reader).Returns(3);

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
            var (handler, readColumn, reader) = CreateHandler(3);

            readColumn.ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 0, reader).Returns(10L, 20L, 30L);
            readColumn.ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 1, reader).Returns(100L, 200L, 300L);
            readColumn.ReadAsString(CommandStringTypes.GetDashboardErrorRetries, 2, reader).Returns("ExA", "ExB", "ExC");
            readColumn.ReadAsInt32(CommandStringTypes.GetDashboardErrorRetries, 3, reader).Returns(1, 2, 3);

            var result = await handler.HandleAsync(new GetDashboardErrorRetriesQuery("100"));

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(10L, result[0].ErrorTrackingId);
            Assert.AreEqual("100", result[0].QueueId);
            Assert.AreEqual(30L, result[2].ErrorTrackingId);
            Assert.AreEqual("300", result[2].QueueId);
            readColumn.Received(3).ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 0, reader);
            readColumn.Received(3).ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 1, reader);
            readColumn.Received(3).ReadAsString(CommandStringTypes.GetDashboardErrorRetries, 2, reader);
            readColumn.Received(3).ReadAsInt32(CommandStringTypes.GetDashboardErrorRetries, 3, reader);
        }

        [TestMethod]
        public async Task HandleAsync_Awaited_Result_Matches_Mocked_Reader_Output()
        {
            var (handler, readColumn, reader) = CreateHandler(1);

            readColumn.ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 0, reader).Returns(99L);
            readColumn.ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 1, reader).Returns(777L);
            readColumn.ReadAsString(CommandStringTypes.GetDashboardErrorRetries, 2, reader).Returns("SpecificException");
            readColumn.ReadAsInt32(CommandStringTypes.GetDashboardErrorRetries, 3, reader).Returns(5);

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
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardErrorRetriesQuery, IReadOnlyList<DashboardErrorRetry>>>();
            var readColumn = Substitute.For<IReadColumn>();

            var connection = Substitute.For<DbConnection>();
            var command = Substitute.For<DbCommand>();
            var reader = Substitute.For<DbDataReader>();
            reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(false);

            factory.Create().Returns(connection);
            connection.CreateCommand().Returns(command);
            command.ExecuteReaderAsync(Arg.Any<CancellationToken>()).Returns(reader);

            var handler = new GetDashboardErrorRetriesQueryHandlerAsync(factory, prepareQuery, readColumn);
            var query = new GetDashboardErrorRetriesQuery("42");

            await handler.HandleAsync(query);

            prepareQuery.Received(1).Handle(query, command, CommandStringTypes.GetDashboardErrorRetries);
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

        private static (GetDashboardErrorRetriesQueryHandlerAsync handler, IReadColumn readColumn, DbDataReader reader) CreateHandler(int rowCount)
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardErrorRetriesQuery, IReadOnlyList<DashboardErrorRetry>>>();
            var readColumn = Substitute.For<IReadColumn>();

            var connection = Substitute.For<DbConnection>();
            var command = Substitute.For<DbCommand>();
            var reader = Substitute.For<DbDataReader>();

            if (rowCount <= 0)
            {
                reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(false);
            }
            else
            {
                var rest = new bool[rowCount];
                for (var i = 0; i < rowCount - 1; i++) rest[i] = true;
                rest[rowCount - 1] = false;
                reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(true, rest);
            }

            factory.Create().Returns(connection);
            connection.CreateCommand().Returns(command);
            command.ExecuteReaderAsync(Arg.Any<CancellationToken>()).Returns(reader);

            var handler = new GetDashboardErrorRetriesQueryHandlerAsync(factory, prepareQuery, readColumn);
            return (handler, readColumn, reader);
        }
    }
}
