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
    public class GetDashboardMessageHeadersQueryHandlerAsyncTests
    {
        [Fact]
        public async Task HandleAsync_Returns_Headers_When_Found()
        {
            var (handler, readColumn, reader) = CreateHandler(true);
            var headerBytes = new byte[] { 7, 8, 9 };

            readColumn.ReadAsByteArray(CommandStringTypes.GetDashboardMessageHeaders, 0, reader).Returns(headerBytes);

            var result = await handler.HandleAsync(new GetDashboardMessageHeadersQuery(42));

            Assert.NotNull(result);
            Assert.Equal(headerBytes, result.Headers);
        }

        [Fact]
        public async Task HandleAsync_Returns_Null_When_Not_Found()
        {
            var (handler, _, _) = CreateHandler(false);

            var result = await handler.HandleAsync(new GetDashboardMessageHeadersQuery(999));

            Assert.Null(result);
        }

        private static (GetDashboardMessageHeadersQueryHandlerAsync handler, IReadColumn readColumn, DbDataReader reader) CreateHandler(bool hasRows)
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardMessageHeadersQuery, DashboardMessageHeaders>>();
            var readColumn = Substitute.For<IReadColumn>();

            var connection = Substitute.For<DbConnection>();
            var command = Substitute.For<DbCommand>();
            var reader = Substitute.For<DbDataReader>();
            reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(hasRows, false);

            factory.Create().Returns(connection);
            connection.CreateCommand().Returns(command);
            command.ExecuteReaderAsync(Arg.Any<CancellationToken>()).Returns(reader);

            var handler = new GetDashboardMessageHeadersQueryHandlerAsync(factory, prepareQuery, readColumn);
            return (handler, readColumn, reader);
        }
    }
}
