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
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryHandler
{
    public class GetDashboardJobsQueryHandlerAsyncTests
    {
        [Fact]
        public async Task HandleAsync_Returns_Jobs()
        {
            var (handler, readColumn, reader) = CreateHandler(1);
            var now = DateTimeOffset.UtcNow;

            readColumn.ReadAsString(CommandStringTypes.GetDashboardJobs, 0, reader).Returns("TestJob");
            readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardJobs, 1, reader).Returns(now);
            readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardJobs, 2, reader).Returns(now.AddMinutes(5));

            var result = await handler.HandleAsync(new GetDashboardJobsQuery());

            Assert.Single(result);
            Assert.Equal("TestJob", result[0].JobName);
            Assert.Equal(now, result[0].JobEventTime);
        }

        [Fact]
        public async Task HandleAsync_Returns_Empty_When_No_Rows()
        {
            var (handler, _, _) = CreateHandler(0);

            var result = await handler.HandleAsync(new GetDashboardJobsQuery());

            Assert.Empty(result);
        }

        private static (GetDashboardJobsQueryHandlerAsync handler, IReadColumn readColumn, DbDataReader reader) CreateHandler(int rowCount)
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardJobsQuery, IReadOnlyList<DashboardJob>>>();
            var readColumn = Substitute.For<IReadColumn>();

            var connection = Substitute.For<DbConnection>();
            var command = Substitute.For<DbCommand>();
            var reader = Substitute.For<DbDataReader>();

            if (rowCount > 0)
                reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(true, false);
            else
                reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(false);

            factory.Create().Returns(connection);
            connection.CreateCommand().Returns(command);
            command.ExecuteReaderAsync(Arg.Any<CancellationToken>()).Returns(reader);

            var handler = new GetDashboardJobsQueryHandlerAsync(factory, prepareQuery, readColumn);
            return (handler, readColumn, reader);
        }
    }
}
