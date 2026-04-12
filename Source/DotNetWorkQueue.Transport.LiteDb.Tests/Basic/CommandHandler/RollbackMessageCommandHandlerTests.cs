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
using DotNetWorkQueue.Transport.LiteDb.Basic.CommandHandler;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.LiteDb.Tests.Basic.CommandHandler
{
    [TestClass]
    public class RollbackMessageCommandHandlerTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var getUtcDateQuery = Substitute.For<IGetTimeFactory>();
            var optionsFactory = Substitute.For<ILiteDbMessageQueueTransportOptionsFactory>();
            var tableNameHelper = CreateTableNameHelper();
            var connectionManager = CreateConnectionManager();
            var databaseExists = CreateDatabaseExists();

            Assert.IsNotNull(new RollbackMessageCommandHandler(
                getUtcDateQuery,
                optionsFactory,
                tableNameHelper,
                connectionManager,
                databaseExists));
        }

        [TestMethod]
        public void Create_NullGetUtcDateQuery_Throws()
        {
            var optionsFactory = Substitute.For<ILiteDbMessageQueueTransportOptionsFactory>();
            var tableNameHelper = CreateTableNameHelper();
            var connectionManager = CreateConnectionManager();
            var databaseExists = CreateDatabaseExists();

            Assert.ThrowsExactly<ArgumentNullException>(
                () => new RollbackMessageCommandHandler(
                    null,
                    optionsFactory,
                    tableNameHelper,
                    connectionManager,
                    databaseExists));
        }

        [TestMethod]
        public void Create_NullOptionsFactory_Throws()
        {
            var getUtcDateQuery = Substitute.For<IGetTimeFactory>();
            var tableNameHelper = CreateTableNameHelper();
            var connectionManager = CreateConnectionManager();
            var databaseExists = CreateDatabaseExists();

            Assert.ThrowsExactly<ArgumentNullException>(
                () => new RollbackMessageCommandHandler(
                    getUtcDateQuery,
                    null,
                    tableNameHelper,
                    connectionManager,
                    databaseExists));
        }

        [TestMethod]
        public void Create_NullTableNameHelper_Throws()
        {
            var getUtcDateQuery = Substitute.For<IGetTimeFactory>();
            var optionsFactory = Substitute.For<ILiteDbMessageQueueTransportOptionsFactory>();
            var connectionManager = CreateConnectionManager();
            var databaseExists = CreateDatabaseExists();

            Assert.ThrowsExactly<ArgumentNullException>(
                () => new RollbackMessageCommandHandler(
                    getUtcDateQuery,
                    optionsFactory,
                    null,
                    connectionManager,
                    databaseExists));
        }

        [TestMethod]
        public void Create_NullConnectionManager_Throws()
        {
            var getUtcDateQuery = Substitute.For<IGetTimeFactory>();
            var optionsFactory = Substitute.For<ILiteDbMessageQueueTransportOptionsFactory>();
            var tableNameHelper = CreateTableNameHelper();
            var databaseExists = CreateDatabaseExists();

            Assert.ThrowsExactly<ArgumentNullException>(
                () => new RollbackMessageCommandHandler(
                    getUtcDateQuery,
                    optionsFactory,
                    tableNameHelper,
                    null,
                    databaseExists));
        }

        [TestMethod]
        public void Create_NullDatabaseExists_Throws()
        {
            var getUtcDateQuery = Substitute.For<IGetTimeFactory>();
            var optionsFactory = Substitute.For<ILiteDbMessageQueueTransportOptionsFactory>();
            var tableNameHelper = CreateTableNameHelper();
            var connectionManager = CreateConnectionManager();

            Assert.ThrowsExactly<ArgumentNullException>(
                () => new RollbackMessageCommandHandler(
                    getUtcDateQuery,
                    optionsFactory,
                    tableNameHelper,
                    connectionManager,
                    null));
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

        private static DatabaseExists CreateDatabaseExists()
        {
            return new DatabaseExists(
                Substitute.For<IGetFileNameFromConnectionString>(),
                Substitute.For<IConnectionInformation>(),
                Substitute.For<ILogger>());
        }
    }
}
