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
using System.IO;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.Basic.CommandHandler;
using DotNetWorkQueue.Transport.LiteDb.Schema;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.Tests.Basic.CommandHandler
{
    [TestClass]
    public class DashboardUpdateMessageBodyCommandHandlerTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var connectionManager = CreateConnectionManager();
            var tableNameHelper = CreateTableNameHelper();
            Assert.IsNotNull(new DashboardUpdateMessageBodyCommandHandler(connectionManager, tableNameHelper));
        }

        [TestMethod]
        public void Create_NullConnectionManager_Throws()
        {
            var tableNameHelper = CreateTableNameHelper();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new DashboardUpdateMessageBodyCommandHandler(null, tableNameHelper));
        }

        [TestMethod]
        public void Create_NullTableNameHelper_Throws()
        {
            var connectionManager = CreateConnectionManager();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new DashboardUpdateMessageBodyCommandHandler(connectionManager, null));
        }

        [TestMethod]
        public void Handle_MessageNotFound_ReturnsZero()
        {
            var (handler, connectionManager, _, dbPath) = CreateHandlerWithDb();
            try
            {
                var body = new byte[] { 1, 2, 3 };
                var headers = new byte[] { 4, 5, 6 };
                var result = handler.Handle(new DashboardUpdateMessageBodyCommand("999", body, headers));
                Assert.AreEqual(0, result);
            }
            finally
            {
                connectionManager.Dispose();
                TryDeleteFile(dbPath);
            }
        }

        [TestMethod]
        public void Handle_EmptyQueue_ReturnsZero()
        {
            var (handler, connectionManager, _, dbPath) = CreateHandlerWithDb();
            try
            {
                var result = handler.Handle(
                    new DashboardUpdateMessageBodyCommand("1", new byte[] { 0 }, new byte[] { 0 }));
                Assert.AreEqual(0, result);
            }
            finally
            {
                connectionManager.Dispose();
                TryDeleteFile(dbPath);
            }
        }

        [TestMethod]
        public void Handle_ExistingMessage_UpdatesBodyAndHeadersAndReturnsOne()
        {
            var (handler, connectionManager, tableNameHelper, dbPath) = CreateHandlerWithDb();
            try
            {
                var originalBody = new byte[] { 10, 20, 30 };
                var originalHeaders = new byte[] { 40, 50, 60 };
                int insertedId;

                using (var db = connectionManager.GetDatabase())
                {
                    var col = db.Database.GetCollection<QueueTable>(tableNameHelper.QueueName);
                    var record = new QueueTable
                    {
                        Body = originalBody,
                        Headers = originalHeaders
                    };
                    insertedId = col.Insert(record).AsInt32;
                }

                var newBody = new byte[] { 1, 2, 3, 4 };
                var newHeaders = new byte[] { 5, 6, 7, 8 };

                var result = handler.Handle(
                    new DashboardUpdateMessageBodyCommand(insertedId.ToString(), newBody, newHeaders));

                Assert.AreEqual(1, result);

                using (var db = connectionManager.GetDatabase())
                {
                    var col = db.Database.GetCollection<QueueTable>(tableNameHelper.QueueName);
                    var updated = col.FindById(insertedId);
                    Assert.IsNotNull(updated);
                    CollectionAssert.AreEqual(newBody, updated.Body);
                    CollectionAssert.AreEqual(newHeaders, updated.Headers);
                }
            }
            finally
            {
                connectionManager.Dispose();
                TryDeleteFile(dbPath);
            }
        }

        [TestMethod]
        public void Handle_UpdatesOnlyTargetedMessage()
        {
            var (handler, connectionManager, tableNameHelper, dbPath) = CreateHandlerWithDb();
            try
            {
                int id1, id2;
                var originalBody2 = new byte[] { 99 };
                var originalHeaders2 = new byte[] { 88 };

                using (var db = connectionManager.GetDatabase())
                {
                    var col = db.Database.GetCollection<QueueTable>(tableNameHelper.QueueName);
                    id1 = col.Insert(new QueueTable
                    {
                        Body = new byte[] { 1 },
                        Headers = new byte[] { 2 }
                    }).AsInt32;
                    id2 = col.Insert(new QueueTable
                    {
                        Body = originalBody2,
                        Headers = originalHeaders2
                    }).AsInt32;
                }

                var newBody = new byte[] { 77, 77 };
                var newHeaders = new byte[] { 66, 66 };

                var result = handler.Handle(
                    new DashboardUpdateMessageBodyCommand(id1.ToString(), newBody, newHeaders));

                Assert.AreEqual(1, result);

                using (var db = connectionManager.GetDatabase())
                {
                    var col = db.Database.GetCollection<QueueTable>(tableNameHelper.QueueName);

                    var updated = col.FindById(id1);
                    CollectionAssert.AreEqual(newBody, updated.Body);
                    CollectionAssert.AreEqual(newHeaders, updated.Headers);

                    var untouched = col.FindById(id2);
                    CollectionAssert.AreEqual(originalBody2, untouched.Body);
                    CollectionAssert.AreEqual(originalHeaders2, untouched.Headers);
                }
            }
            finally
            {
                connectionManager.Dispose();
                TryDeleteFile(dbPath);
            }
        }

        [TestMethod]
        public void Handle_ExistingMessage_WithNullBodyAndHeaders_WritesNulls()
        {
            var (handler, connectionManager, tableNameHelper, dbPath) = CreateHandlerWithDb();
            try
            {
                int insertedId;
                using (var db = connectionManager.GetDatabase())
                {
                    var col = db.Database.GetCollection<QueueTable>(tableNameHelper.QueueName);
                    insertedId = col.Insert(new QueueTable
                    {
                        Body = new byte[] { 1, 2, 3 },
                        Headers = new byte[] { 4, 5, 6 }
                    }).AsInt32;
                }

                var result = handler.Handle(
                    new DashboardUpdateMessageBodyCommand(insertedId.ToString(), null, null));

                Assert.AreEqual(1, result);

                using (var db = connectionManager.GetDatabase())
                {
                    var col = db.Database.GetCollection<QueueTable>(tableNameHelper.QueueName);
                    var updated = col.FindById(insertedId);
                    Assert.IsNotNull(updated);
                    Assert.IsNull(updated.Body);
                    Assert.IsNull(updated.Headers);
                }
            }
            finally
            {
                connectionManager.Dispose();
                TryDeleteFile(dbPath);
            }
        }

        [TestMethod]
        public void Handle_NonNumericMessageId_Throws()
        {
            var (handler, connectionManager, _, dbPath) = CreateHandlerWithDb();
            try
            {
                Assert.ThrowsExactly<FormatException>(() =>
                    handler.Handle(new DashboardUpdateMessageBodyCommand("not-a-number", new byte[] { 0 }, new byte[] { 0 })));
            }
            finally
            {
                connectionManager.Dispose();
                TryDeleteFile(dbPath);
            }
        }

        private static (DashboardUpdateMessageBodyCommandHandler handler, LiteDbConnectionManager connectionManager, TableNameHelper tableNameHelper, string dbPath)
            CreateHandlerWithDb()
        {
            var queueName = "TestQueue";
            var dbPath = Path.Combine(Path.GetTempPath(), $"litedb_test_{Guid.NewGuid():N}.db");
            var connectionString = $"Filename={dbPath};Connection=direct";
            var connectionInfo = new LiteDbConnectionInformation(
                new QueueConnection(queueName, connectionString));
            var scope = Substitute.For<ICreationScope>();
            var connectionManager = new LiteDbConnectionManager(connectionInfo, scope);
            var tableNameHelper = new TableNameHelper(connectionInfo);

            var handler = new DashboardUpdateMessageBodyCommandHandler(connectionManager, tableNameHelper);
            return (handler, connectionManager, tableNameHelper, dbPath);
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

        private static void TryDeleteFile(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort cleanup */ }
        }
    }
}
