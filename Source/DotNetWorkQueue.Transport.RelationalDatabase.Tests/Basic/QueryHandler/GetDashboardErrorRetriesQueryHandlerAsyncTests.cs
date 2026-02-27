// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler;
using DotNetWorkQueue.Transport.Shared;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryHandler
{
    public class GetDashboardErrorRetriesQueryHandlerAsyncTests
    {
        [Fact]
        public async Task HandleAsync_Returns_Retries()
        {
            var (handler, readColumn, reader) = CreateHandler(1);

            readColumn.ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 0, reader).Returns(10L);
            readColumn.ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 1, reader).Returns(42L);
            readColumn.ReadAsString(CommandStringTypes.GetDashboardErrorRetries, 2, reader).Returns("TimeoutException");
            readColumn.ReadAsInt32(CommandStringTypes.GetDashboardErrorRetries, 3, reader).Returns(3);

            var result = await handler.HandleAsync(new GetDashboardErrorRetriesQuery(42));

            Assert.Single(result);
            Assert.Equal(10L, result[0].ErrorTrackingId);
            Assert.Equal(42L, result[0].QueueId);
            Assert.Equal("TimeoutException", result[0].ExceptionType);
            Assert.Equal(3, result[0].RetryCount);
        }

        [Fact]
        public async Task HandleAsync_Returns_Empty_When_No_Rows()
        {
            var (handler, _, _) = CreateHandler(0);

            var result = await handler.HandleAsync(new GetDashboardErrorRetriesQuery(42));

            Assert.Empty(result);
        }

        private static (GetDashboardErrorRetriesQueryHandlerAsync handler, IReadColumn readColumn, DbDataReader reader) CreateHandler(int rowCount)
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardErrorRetriesQuery, IReadOnlyList<DashboardErrorRetry>>>();
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

            var handler = new GetDashboardErrorRetriesQueryHandlerAsync(factory, prepareQuery, readColumn);
            return (handler, readColumn, reader);
        }
    }
}
