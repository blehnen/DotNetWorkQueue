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
    public class GetDashboardStaleMessagesQueryHandlerAsyncTests
    {
        [Fact]
        public async Task HandleAsync_Returns_Messages_From_Reader()
        {
            var (handler, readColumn, reader) = CreateHandler(1);

            readColumn.ReadAsInt64(CommandStringTypes.GetDashboardStaleMessages, 0, reader).Returns(1L);
            readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardStaleMessages, 1, reader).Returns(DateTimeOffset.UtcNow);
            readColumn.ReadAsString(CommandStringTypes.GetDashboardStaleMessages, 2, reader).Returns("corr-1");

            var result = await handler.HandleAsync(new GetDashboardStaleMessagesQuery(60, 0, 25));

            Assert.Single(result);
            Assert.Equal("1", result[0].QueueId);
        }

        [Fact]
        public async Task HandleAsync_Returns_Empty_When_No_Rows()
        {
            var (handler, _, _) = CreateHandler(0);

            var result = await handler.HandleAsync(new GetDashboardStaleMessagesQuery(60, 0, 25));

            Assert.Empty(result);
        }

        private static (GetDashboardStaleMessagesQueryHandlerAsync handler, IReadColumn readColumn, DbDataReader reader) CreateHandler(int rowCount)
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardStaleMessagesQuery, IReadOnlyList<DashboardMessage>>>();
            var readColumn = Substitute.For<IReadColumn>();
            var optionsFactory = Substitute.For<ITransportOptionsFactory>();
            var options = Substitute.For<ITransportOptions>();
            optionsFactory.Create().Returns(options);

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

            var handler = new GetDashboardStaleMessagesQueryHandlerAsync(factory, prepareQuery, readColumn, optionsFactory);
            return (handler, readColumn, reader);
        }
    }
}
