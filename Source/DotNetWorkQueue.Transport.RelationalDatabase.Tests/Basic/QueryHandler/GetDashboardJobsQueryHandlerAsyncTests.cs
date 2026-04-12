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
    public class GetDashboardJobsQueryHandlerAsyncTests
    {
        [TestMethod]
        public async Task HandleAsync_Returns_Jobs()
        {
            var (handler, readColumn, reader) = CreateHandler(1);
            var now = DateTimeOffset.UtcNow;

            readColumn.ReadAsString(CommandStringTypes.GetDashboardJobs, 0, reader).Returns("TestJob");
            readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardJobs, 1, reader).Returns(now);
            readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardJobs, 2, reader).Returns(now.AddMinutes(5));

            var result = await handler.HandleAsync(new GetDashboardJobsQuery());

            Assert.ContainsSingle(result);
            Assert.AreEqual("TestJob", result[0].JobName);
            Assert.AreEqual(now, result[0].JobEventTime);
        }

        [TestMethod]
        public async Task HandleAsync_Returns_Empty_When_No_Rows()
        {
            var (handler, _, _) = CreateHandler(0);

            var result = await handler.HandleAsync(new GetDashboardJobsQuery());

            Assert.IsEmpty(result);
        }

        [TestMethod]
        public async Task HandleAsync_Returns_Multiple_Jobs_From_Reader()
        {
            var (handler, readColumn, reader) = CreateHandler(3);
            var now = DateTimeOffset.UtcNow;

            readColumn.ReadAsString(CommandStringTypes.GetDashboardJobs, 0, reader).Returns("JobA", "JobB", "JobC");
            readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardJobs, 1, reader).Returns(now, now.AddMinutes(1), now.AddMinutes(2));
            readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardJobs, 2, reader).Returns(now.AddMinutes(5), now.AddMinutes(6), now.AddMinutes(7));

            var result = await handler.HandleAsync(new GetDashboardJobsQuery());

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("JobA", result[0].JobName);
            Assert.AreEqual("JobB", result[1].JobName);
            Assert.AreEqual("JobC", result[2].JobName);
            readColumn.Received(3).ReadAsString(CommandStringTypes.GetDashboardJobs, 0, reader);
            readColumn.Received(3).ReadAsDateTimeOffset(CommandStringTypes.GetDashboardJobs, 1, reader);
            readColumn.Received(3).ReadAsDateTimeOffset(CommandStringTypes.GetDashboardJobs, 2, reader);
        }

        [TestMethod]
        public async Task HandleAsync_Awaited_Result_Matches_Mocked_Reader_Output()
        {
            var (handler, readColumn, reader) = CreateHandler(1);
            var expectedEvent = new DateTimeOffset(2026, 4, 12, 10, 30, 0, TimeSpan.Zero);
            var expectedScheduled = expectedEvent.AddMinutes(15);

            readColumn.ReadAsString(CommandStringTypes.GetDashboardJobs, 0, reader).Returns("SpecificJob");
            readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardJobs, 1, reader).Returns(expectedEvent);
            readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardJobs, 2, reader).Returns(expectedScheduled);

            var task = handler.HandleAsync(new GetDashboardJobsQuery());
            var awaited = await task;

            Assert.IsTrue(task.IsCompletedSuccessfully);
            Assert.ContainsSingle(awaited);
            Assert.AreEqual("SpecificJob", awaited[0].JobName);
            Assert.AreEqual(expectedEvent, awaited[0].JobEventTime);
            Assert.AreEqual(expectedScheduled, awaited[0].JobScheduledTime);
        }

        [TestMethod]
        public async Task HandleAsync_Invokes_PrepareQuery_With_Correct_CommandString()
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardJobsQuery, IReadOnlyList<DashboardJob>>>();
            var readColumn = Substitute.For<IReadColumn>();

            var connection = Substitute.For<DbConnection>();
            var command = Substitute.For<DbCommand>();
            var reader = Substitute.For<DbDataReader>();
            reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(false);

            factory.Create().Returns(connection);
            connection.CreateCommand().Returns(command);
            command.ExecuteReaderAsync(Arg.Any<CancellationToken>()).Returns(reader);

            var handler = new GetDashboardJobsQueryHandlerAsync(factory, prepareQuery, readColumn);
            var query = new GetDashboardJobsQuery();

            await handler.HandleAsync(query);

            prepareQuery.Received(1).Handle(query, command, CommandStringTypes.GetDashboardJobs);
        }

        [TestMethod]
        public void Constructor_Throws_When_DbConnectionFactory_Is_Null()
        {
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardJobsQuery, IReadOnlyList<DashboardJob>>>();
            var readColumn = Substitute.For<IReadColumn>();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new GetDashboardJobsQueryHandlerAsync(null, prepareQuery, readColumn));
        }

        [TestMethod]
        public void Constructor_Throws_When_PrepareQuery_Is_Null()
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var readColumn = Substitute.For<IReadColumn>();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new GetDashboardJobsQueryHandlerAsync(factory, null, readColumn));
        }

        [TestMethod]
        public void Constructor_Throws_When_ReadColumn_Is_Null()
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardJobsQuery, IReadOnlyList<DashboardJob>>>();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new GetDashboardJobsQueryHandlerAsync(factory, prepareQuery, null));
        }

        private static (GetDashboardJobsQueryHandlerAsync handler, IReadColumn readColumn, DbDataReader reader) CreateHandler(int rowCount)
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardJobsQuery, IReadOnlyList<DashboardJob>>>();
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

            var handler = new GetDashboardJobsQueryHandlerAsync(factory, prepareQuery, readColumn);
            return (handler, readColumn, reader);
        }
    }
}
