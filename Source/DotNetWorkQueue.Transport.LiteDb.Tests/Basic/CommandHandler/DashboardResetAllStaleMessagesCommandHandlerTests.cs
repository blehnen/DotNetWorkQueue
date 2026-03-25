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
    public class DashboardResetAllStaleMessagesCommandHandlerTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var (handler, _, _) = CreateHandler();
            Assert.IsNotNull(handler);
        }

        [TestMethod]
        public void Create_NullOptionsFactory_Throws()
        {
            var connectionManager = CreateConnectionManager();
            var tableNameHelper = CreateTableNameHelper();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new DashboardResetAllStaleMessagesCommandHandler(null, connectionManager, tableNameHelper));
        }

        [TestMethod]
        public void Create_NullConnectionManager_Throws()
        {
            var optionsFactory = Substitute.For<ILiteDbMessageQueueTransportOptionsFactory>();
            var tableNameHelper = CreateTableNameHelper();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new DashboardResetAllStaleMessagesCommandHandler(optionsFactory, null, tableNameHelper));
        }

        [TestMethod]
        public void Create_NullTableNameHelper_Throws()
        {
            var optionsFactory = Substitute.For<ILiteDbMessageQueueTransportOptionsFactory>();
            var connectionManager = CreateConnectionManager();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new DashboardResetAllStaleMessagesCommandHandler(optionsFactory, connectionManager, null));
        }

        [TestMethod]
        public void Handle_NoStaleRecords_ReturnsZero()
        {
            var (handler, connectionManager, _, dbPath) = CreateHandlerWithDb();
            try
            {
                var result = handler.Handle(new DashboardResetAllStaleMessagesCommand());
                Assert.AreEqual(0, result);
            }
            finally
            {
                connectionManager.Dispose();
                TryDeleteFile(dbPath);
            }
        }

        [TestMethod]
        public void Handle_NoProcessingRecords_ReturnsZero()
        {
            var (handler, connectionManager, tableNameHelper, dbPath) = CreateHandlerWithDb();
            try
            {
                using (var db = connectionManager.GetDatabase())
                {
                    var metaCol = db.Database.GetCollection<MetaDataTable>(tableNameHelper.MetaDataName);
                    metaCol.Insert(new MetaDataTable
                    {
                        QueueId = 1,
                        CorrelationId = Guid.NewGuid(),
                        Status = QueueStatuses.Waiting,
                        QueuedDateTime = DateTime.UtcNow
                    });
                }

                var result = handler.Handle(new DashboardResetAllStaleMessagesCommand());
                Assert.AreEqual(0, result);
            }
            finally
            {
                connectionManager.Dispose();
                TryDeleteFile(dbPath);
            }
        }

        [TestMethod]
        public void Handle_WithProcessingRecords_ResetsToWaiting()
        {
            var (handler, connectionManager, tableNameHelper, dbPath) = CreateHandlerWithDb();
            try
            {
                using (var db = connectionManager.GetDatabase())
                {
                    var metaCol = db.Database.GetCollection<MetaDataTable>(tableNameHelper.MetaDataName);
                    metaCol.Insert(new MetaDataTable
                    {
                        QueueId = 1,
                        CorrelationId = Guid.NewGuid(),
                        Status = QueueStatuses.Processing,
                        QueuedDateTime = DateTime.UtcNow,
                        HeartBeat = DateTime.UtcNow.AddMinutes(-10)
                    });
                }

                var result = handler.Handle(new DashboardResetAllStaleMessagesCommand());
                Assert.AreEqual(1, result);

                using (var db = connectionManager.GetDatabase())
                {
                    var metaCol = db.Database.GetCollection<MetaDataTable>(tableNameHelper.MetaDataName);
                    var updated = metaCol.Query().Where(x => x.QueueId == 1).FirstOrDefault();
                    Assert.IsNotNull(updated);
                    Assert.AreEqual(QueueStatuses.Waiting, updated.Status);
                    Assert.IsNull(updated.HeartBeat);
                }
            }
            finally
            {
                connectionManager.Dispose();
                TryDeleteFile(dbPath);
            }
        }

        [TestMethod]
        public void Handle_MultipleProcessingRecords_ResetsAll()
        {
            var (handler, connectionManager, tableNameHelper, dbPath) = CreateHandlerWithDb();
            try
            {
                using (var db = connectionManager.GetDatabase())
                {
                    var metaCol = db.Database.GetCollection<MetaDataTable>(tableNameHelper.MetaDataName);
                    for (int i = 1; i <= 5; i++)
                    {
                        metaCol.Insert(new MetaDataTable
                        {
                            QueueId = i,
                            CorrelationId = Guid.NewGuid(),
                            Status = QueueStatuses.Processing,
                            QueuedDateTime = DateTime.UtcNow,
                            HeartBeat = DateTime.UtcNow.AddMinutes(-5)
                        });
                    }
                }

                var result = handler.Handle(new DashboardResetAllStaleMessagesCommand());
                Assert.AreEqual(5, result);
            }
            finally
            {
                connectionManager.Dispose();
                TryDeleteFile(dbPath);
            }
        }

        [TestMethod]
        public void Handle_MixedStatuses_OnlyResetsProcessing()
        {
            var (handler, connectionManager, tableNameHelper, dbPath) = CreateHandlerWithDb();
            try
            {
                using (var db = connectionManager.GetDatabase())
                {
                    var metaCol = db.Database.GetCollection<MetaDataTable>(tableNameHelper.MetaDataName);

                    metaCol.Insert(new MetaDataTable
                    {
                        QueueId = 1,
                        CorrelationId = Guid.NewGuid(),
                        Status = QueueStatuses.Processing,
                        QueuedDateTime = DateTime.UtcNow,
                        HeartBeat = DateTime.UtcNow
                    });

                    metaCol.Insert(new MetaDataTable
                    {
                        QueueId = 2,
                        CorrelationId = Guid.NewGuid(),
                        Status = QueueStatuses.Waiting,
                        QueuedDateTime = DateTime.UtcNow
                    });

                    metaCol.Insert(new MetaDataTable
                    {
                        QueueId = 3,
                        CorrelationId = Guid.NewGuid(),
                        Status = QueueStatuses.Error,
                        QueuedDateTime = DateTime.UtcNow
                    });
                }

                var result = handler.Handle(new DashboardResetAllStaleMessagesCommand());
                Assert.AreEqual(1, result);

                using (var db = connectionManager.GetDatabase())
                {
                    var metaCol = db.Database.GetCollection<MetaDataTable>(tableNameHelper.MetaDataName);

                    var record1 = metaCol.Query().Where(x => x.QueueId == 1).FirstOrDefault();
                    Assert.AreEqual(QueueStatuses.Waiting, record1.Status);

                    var record2 = metaCol.Query().Where(x => x.QueueId == 2).FirstOrDefault();
                    Assert.AreEqual(QueueStatuses.Waiting, record2.Status);

                    var record3 = metaCol.Query().Where(x => x.QueueId == 3).FirstOrDefault();
                    Assert.AreEqual(QueueStatuses.Error, record3.Status);
                }
            }
            finally
            {
                connectionManager.Dispose();
                TryDeleteFile(dbPath);
            }
        }

        [TestMethod]
        public void Handle_StatusTableEnabled_UpdatesStatusRecords()
        {
            var (handler, connectionManager, tableNameHelper, dbPath) = CreateHandlerWithDb(enableStatusTable: true);
            try
            {
                using (var db = connectionManager.GetDatabase())
                {
                    var metaCol = db.Database.GetCollection<MetaDataTable>(tableNameHelper.MetaDataName);
                    metaCol.Insert(new MetaDataTable
                    {
                        QueueId = 1,
                        CorrelationId = Guid.NewGuid(),
                        Status = QueueStatuses.Processing,
                        QueuedDateTime = DateTime.UtcNow,
                        HeartBeat = DateTime.UtcNow
                    });

                    var statusCol = db.Database.GetCollection<StatusTable>(tableNameHelper.StatusName);
                    statusCol.Insert(new StatusTable
                    {
                        QueueId = 1,
                        CorrelationId = Guid.NewGuid(),
                        Status = QueueStatuses.Processing
                    });
                }

                var result = handler.Handle(new DashboardResetAllStaleMessagesCommand());
                Assert.AreEqual(1, result);

                using (var db = connectionManager.GetDatabase())
                {
                    var statusCol = db.Database.GetCollection<StatusTable>(tableNameHelper.StatusName);
                    var statusRecord = statusCol.Query().Where(x => x.QueueId == 1).FirstOrDefault();
                    Assert.IsNotNull(statusRecord);
                    Assert.AreEqual(QueueStatuses.Waiting, statusRecord.Status);
                }
            }
            finally
            {
                connectionManager.Dispose();
                TryDeleteFile(dbPath);
            }
        }

        [TestMethod]
        public void Handle_StatusTableEnabled_NoMatchingStatusRecord_DoesNotThrow()
        {
            var (handler, connectionManager, tableNameHelper, dbPath) = CreateHandlerWithDb(enableStatusTable: true);
            try
            {
                using (var db = connectionManager.GetDatabase())
                {
                    var metaCol = db.Database.GetCollection<MetaDataTable>(tableNameHelper.MetaDataName);
                    metaCol.Insert(new MetaDataTable
                    {
                        QueueId = 1,
                        CorrelationId = Guid.NewGuid(),
                        Status = QueueStatuses.Processing,
                        QueuedDateTime = DateTime.UtcNow,
                        HeartBeat = DateTime.UtcNow
                    });
                }

                var result = handler.Handle(new DashboardResetAllStaleMessagesCommand());
                Assert.AreEqual(1, result);
            }
            finally
            {
                connectionManager.Dispose();
                TryDeleteFile(dbPath);
            }
        }

        [TestMethod]
        public void Handle_StatusTableDisabled_DoesNotTouchStatusTable()
        {
            var (handler, connectionManager, tableNameHelper, dbPath) = CreateHandlerWithDb(enableStatusTable: false);
            try
            {
                using (var db = connectionManager.GetDatabase())
                {
                    var metaCol = db.Database.GetCollection<MetaDataTable>(tableNameHelper.MetaDataName);
                    metaCol.Insert(new MetaDataTable
                    {
                        QueueId = 1,
                        CorrelationId = Guid.NewGuid(),
                        Status = QueueStatuses.Processing,
                        QueuedDateTime = DateTime.UtcNow,
                        HeartBeat = DateTime.UtcNow
                    });

                    var statusCol = db.Database.GetCollection<StatusTable>(tableNameHelper.StatusName);
                    statusCol.Insert(new StatusTable
                    {
                        QueueId = 1,
                        CorrelationId = Guid.NewGuid(),
                        Status = QueueStatuses.Processing
                    });
                }

                handler.Handle(new DashboardResetAllStaleMessagesCommand());

                using (var db = connectionManager.GetDatabase())
                {
                    var statusCol = db.Database.GetCollection<StatusTable>(tableNameHelper.StatusName);
                    var statusRecord = statusCol.Query().Where(x => x.QueueId == 1).FirstOrDefault();
                    Assert.IsNotNull(statusRecord);
                    Assert.AreEqual(QueueStatuses.Processing, statusRecord.Status);
                }
            }
            finally
            {
                connectionManager.Dispose();
                TryDeleteFile(dbPath);
            }
        }

        private static (DashboardResetAllStaleMessagesCommandHandler handler, LiteDbConnectionManager connectionManager, TableNameHelper tableNameHelper)
            CreateHandler()
        {
            var optionsFactory = Substitute.For<ILiteDbMessageQueueTransportOptionsFactory>();
            optionsFactory.Create().Returns(new LiteDbMessageQueueTransportOptions());
            var connectionManager = CreateConnectionManager();
            var tableNameHelper = CreateTableNameHelper();
            var handler = new DashboardResetAllStaleMessagesCommandHandler(optionsFactory, connectionManager, tableNameHelper);
            return (handler, connectionManager, tableNameHelper);
        }

        private static (DashboardResetAllStaleMessagesCommandHandler handler, LiteDbConnectionManager connectionManager, TableNameHelper tableNameHelper, string dbPath)
            CreateHandlerWithDb(bool enableStatusTable = false)
        {
            var queueName = $"TestQueue";
            var dbPath = Path.Combine(Path.GetTempPath(), $"litedb_test_{Guid.NewGuid():N}.db");
            var connectionString = $"Filename={dbPath};Connection=direct";
            var connectionInfo = new LiteDbConnectionInformation(
                new QueueConnection(queueName, connectionString));
            var scope = Substitute.For<ICreationScope>();
            var connectionManager = new LiteDbConnectionManager(connectionInfo, scope);
            var tableNameHelper = new TableNameHelper(connectionInfo);

            var options = new LiteDbMessageQueueTransportOptions { EnableStatusTable = enableStatusTable };
            var optionsFactory = Substitute.For<ILiteDbMessageQueueTransportOptionsFactory>();
            optionsFactory.Create().Returns(options);

            var handler = new DashboardResetAllStaleMessagesCommandHandler(optionsFactory, connectionManager, tableNameHelper);
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
