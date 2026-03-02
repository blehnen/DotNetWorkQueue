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
    public class GetDashboardMessageDetailQueryHandlerAsyncTests
    {
        [Fact]
        public async Task HandleAsync_Returns_Message_When_Found()
        {
            var (handler, readColumn, reader) = CreateHandler(true);
            var now = DateTimeOffset.UtcNow;

            readColumn.ReadAsInt64(CommandStringTypes.GetDashboardMessageDetail, 0, reader).Returns(42L);
            readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardMessageDetail, 1, reader).Returns(now);
            readColumn.ReadAsString(CommandStringTypes.GetDashboardMessageDetail, 2, reader).Returns("corr-42");

            var result = await handler.HandleAsync(new GetDashboardMessageDetailQuery("42"));

            Assert.NotNull(result);
            Assert.Equal("42", result.QueueId);
            Assert.Equal("corr-42", result.CorrelationId);
        }

        [Fact]
        public async Task HandleAsync_Returns_Null_When_Not_Found()
        {
            var (handler, _, _) = CreateHandler(false);

            var result = await handler.HandleAsync(new GetDashboardMessageDetailQuery("999"));

            Assert.Null(result);
        }

        private static (GetDashboardMessageDetailQueryHandlerAsync handler, IReadColumn readColumn, DbDataReader reader) CreateHandler(bool hasRows)
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardMessageDetailQuery, DashboardMessage>>();
            var readColumn = Substitute.For<IReadColumn>();
            var optionsFactory = Substitute.For<ITransportOptionsFactory>();
            var options = Substitute.For<ITransportOptions>();
            optionsFactory.Create().Returns(options);

            var connection = Substitute.For<DbConnection>();
            var command = Substitute.For<DbCommand>();
            var reader = Substitute.For<DbDataReader>();
            reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(hasRows, false);

            factory.Create().Returns(connection);
            connection.CreateCommand().Returns(command);
            command.ExecuteReaderAsync(Arg.Any<CancellationToken>()).Returns(reader);

            var handler = new GetDashboardMessageDetailQueryHandlerAsync(factory, prepareQuery, readColumn, optionsFactory);
            return (handler, readColumn, reader);
        }
    }
}
