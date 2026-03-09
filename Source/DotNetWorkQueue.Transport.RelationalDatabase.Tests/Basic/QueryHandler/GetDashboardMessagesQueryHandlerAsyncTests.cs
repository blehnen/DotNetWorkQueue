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
    public class GetDashboardMessagesQueryHandlerAsyncTests
    {
        [TestMethod]
        public async Task HandleAsync_Returns_Messages_From_Reader()
        {
            var (handler, readColumn, reader) = CreateHandler(2);
            var now = DateTimeOffset.UtcNow;

            readColumn.ReadAsInt64(CommandStringTypes.GetDashboardMessages, 0, reader).Returns(1L, 2L);
            readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardMessages, 1, reader).Returns(now);
            readColumn.ReadAsString(CommandStringTypes.GetDashboardMessages, 2, reader).Returns("corr-1", "corr-2");

            var result = await handler.HandleAsync(new GetDashboardMessagesQuery(0, 25, null));

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("1", result[0].QueueId);
            Assert.AreEqual("corr-1", result[0].CorrelationId);
        }

        [TestMethod]
        public async Task HandleAsync_Returns_Empty_List_When_No_Rows()
        {
            var (handler, _, _) = CreateHandler(0);

            var result = await handler.HandleAsync(new GetDashboardMessagesQuery(0, 25, null));

            Assert.IsEmpty(result);
        }

        private static (GetDashboardMessagesQueryHandlerAsync handler, IReadColumn readColumn, DbDataReader reader) CreateHandler(int rowCount)
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardMessagesQuery, IReadOnlyList<DashboardMessage>>>();
            var readColumn = Substitute.For<IReadColumn>();
            var optionsFactory = Substitute.For<ITransportOptionsFactory>();
            var options = Substitute.For<ITransportOptions>();
            optionsFactory.Create().Returns(options);

            var connection = Substitute.For<DbConnection>();
            var command = Substitute.For<DbCommand>();
            var reader = Substitute.For<DbDataReader>();

            if (rowCount == 0)
                reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(false);
            else if (rowCount == 1)
                reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(true, false);
            else
                reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(true, true, false);

            factory.Create().Returns(connection);
            connection.CreateCommand().Returns(command);
            command.ExecuteReaderAsync(Arg.Any<CancellationToken>()).Returns(reader);

            var handler = new GetDashboardMessagesQueryHandlerAsync(factory, prepareQuery, readColumn, optionsFactory);
            return (handler, readColumn, reader);
        }
    }
}
