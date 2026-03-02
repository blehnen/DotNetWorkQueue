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
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.Basic.QueryHandler;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.Tests.Basic.QueryHandler
{
    public class GetDashboardConfigurationQueryHandlerAsyncTests
    {
        [Fact]
        public void Create_Default()
        {
            var connectionManager = CreateConnectionManager();
            var tableNameHelper = CreateTableNameHelper();
            Assert.NotNull(new GetDashboardConfigurationQueryHandlerAsync(connectionManager, tableNameHelper));
        }

        [Fact]
        public void Create_NullConnectionManager_Throws()
        {
            var tableNameHelper = CreateTableNameHelper();
            Assert.Throws<ArgumentNullException>(
                () => new GetDashboardConfigurationQueryHandlerAsync(null, tableNameHelper));
        }

        [Fact]
        public void Create_NullTableNameHelper_Throws()
        {
            var connectionManager = CreateConnectionManager();
            Assert.Throws<ArgumentNullException>(
                () => new GetDashboardConfigurationQueryHandlerAsync(connectionManager, null));
        }

        private static LiteDbConnectionManager CreateConnectionManager()
        {
            return new LiteDbConnectionManager(
                Substitute.For<IConnectionInformation>(),
                Substitute.For<ICreationScope>());
        }

        private static TableNameHelper CreateTableNameHelper()
        {
            return new TableNameHelper(Substitute.For<IConnectionInformation>());
        }
    }
}
