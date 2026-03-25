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
    public class DashboardRequeueAllErrorMessagesCommandHandlerTests
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
                () => new DashboardRequeueAllErrorMessagesCommandHandler(null, connectionManager, tableNameHelper));
        }

        [TestMethod]
        public void Create_NullConnectionManager_Throws()
        {
            var optionsFactory = Substitute.For<ILiteDbMessageQueueTransportOptionsFactory>();
            var tableNameHelper = CreateTableNameHelper();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new DashboardRequeueAllErrorMessagesCommandHandler(optionsFactory, null, tableNameHelper));
        }

        [TestMethod]
        public void Create_NullTableNameHelper_Throws()
        {
            var optionsFactory = Substitute.For<ILiteDbMessageQueueTransportOptionsFactory>();
            var connectionManager = CreateConnectionManager();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new DashboardRequeueAllErrorMessagesCommandHandler(optionsFactory, connectionManager, null));
        }

        [TestMethod]
        public void Handle_NoErrors_ReturnsZero()
        {
            var (handler, connectionManager, _, dbPath) = CreateHandlerWithDb();
            try
            {
                var result = handler.Handle(new DashboardRequeueAllErrorMessagesCommand());
                Assert.AreEqual(0, result);
            }
            finally
            {
                connectionManager.Dispose();
                TryDeleteFile(dbPath);
            }
        }

        [TestMethod]
        public void Handle_WithErrorRecords_ExistingMetaData_RequeuesAndReturnsCount()
        {
            var (handler, connectionManager, tableNameHelper, dbPath) = CreateHandlerWithDb();
            try
            {
                using (var db = connectionManager.GetDatabase())
                {
                    var errorCol = db.Database.GetCollection<MetaDataErrorsTable>(tableNameHelper.MetaDataErrorsName);
                    var metaCol = db.Database.GetCollection<MetaDataTable>(tableNameHelper.MetaDataName);

                    var errorRecord = new MetaDataErrorsTable
                    {
                        QueueId = 1,
                        CorrelationId = Guid.NewGuid(),
                        Status = QueueStatuses.Error,
                        QueuedDateTime = DateTime.UtcNow,
                        LastException = "Test exception",
                        LastExceptionDate = DateTime.UtcNow
                    };
                    errorCol.Insert(errorRecord);

                    var metaRecord = new MetaDataTable
                    {
                        QueueId = 1,
                        CorrelationId = errorRecord.CorrelationId,
                        Status = QueueStatuses.Error,
                        QueuedDateTime = errorRecord.QueuedDateTime,
                        HeartBeat = DateTime.UtcNow
                    };
                    metaCol.Insert(metaRecord);
                }

                var result = handler.Handle(new DashboardRequeueAllErrorMessagesCommand());
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
        public void Handle_WithErrorRecords_NoExistingMetaData_InsertsNewMetaData()
        {
            var (handler, connectionManager, tableNameHelper, dbPath) = CreateHandlerWithDb();
            try
            {
                var correlationId = Guid.NewGuid();
                var queuedTime = DateTime.UtcNow;
                var route = "test-route";

                using (var db = connectionManager.GetDatabase())
                {
                    var errorCol = db.Database.GetCollection<MetaDataErrorsTable>(tableNameHelper.MetaDataErrorsName);
                    errorCol.Insert(new MetaDataErrorsTable
                    {
                        QueueId = 10,
                        CorrelationId = correlationId,
                        Status = QueueStatuses.Error,
                        QueuedDateTime = queuedTime,
                        Route = route,
                        LastException = "Test exception",
                        LastExceptionDate = DateTime.UtcNow
                    });
                }

                var result = handler.Handle(new DashboardRequeueAllErrorMessagesCommand());
                Assert.AreEqual(1, result);

                using (var db = connectionManager.GetDatabase())
                {
                    var metaCol = db.Database.GetCollection<MetaDataTable>(tableNameHelper.MetaDataName);
                    var inserted = metaCol.Query().Where(x => x.QueueId == 10).FirstOrDefault();
                    Assert.IsNotNull(inserted);
                    Assert.AreEqual(QueueStatuses.Waiting, inserted.Status);
                    Assert.AreEqual(correlationId, inserted.CorrelationId);
                    Assert.AreEqual(route, inserted.Route);
                    Assert.IsNull(inserted.HeartBeat);
                }
            }
            finally
            {
                connectionManager.Dispose();
                TryDeleteFile(dbPath);
            }
        }

        [TestMethod]
        public void Handle_WithErrorRecords_DeletesErrorTrackingRecords()
        {
            var (handler, connectionManager, tableNameHelper, dbPath) = CreateHandlerWithDb();
            try
            {
                using (var db = connectionManager.GetDatabase())
                {
                    var errorCol = db.Database.GetCollection<MetaDataErrorsTable>(tableNameHelper.MetaDataErrorsName);
                    errorCol.Insert(new MetaDataErrorsTable
                    {
                        QueueId = 1,
                        CorrelationId = Guid.NewGuid(),
                        Status = QueueStatuses.Error,
                        QueuedDateTime = DateTime.UtcNow,
                        LastException = "err",
                        LastExceptionDate = DateTime.UtcNow
                    });

                    var errorTrack = db.Database.GetCollection<ErrorTrackingTable>(tableNameHelper.ErrorTrackingName);
                    errorTrack.Insert(new ErrorTrackingTable { QueueId = 1, RetryCount = 3, ExceptionType = "System.Exception" });
                }

                handler.Handle(new DashboardRequeueAllErrorMessagesCommand());

                using (var db = connectionManager.GetDatabase())
                {
                    var errorTrack = db.Database.GetCollection<ErrorTrackingTable>(tableNameHelper.ErrorTrackingName);
                    Assert.AreEqual(0, errorTrack.Count());
                }
            }
            finally
            {
                connectionManager.Dispose();
                TryDeleteFile(dbPath);
            }
        }

        [TestMethod]
        public void Handle_WithErrorRecords_DeletesErrorMetaDataRecords()
        {
            var (handler, connectionManager, tableNameHelper, dbPath) = CreateHandlerWithDb();
            try
            {
                using (var db = connectionManager.GetDatabase())
                {
                    var errorCol = db.Database.GetCollection<MetaDataErrorsTable>(tableNameHelper.MetaDataErrorsName);
                    errorCol.Insert(new MetaDataErrorsTable
                    {
                        QueueId = 1,
                        CorrelationId = Guid.NewGuid(),
                        Status = QueueStatuses.Error,
                        QueuedDateTime = DateTime.UtcNow,
                        LastException = "err",
                        LastExceptionDate = DateTime.UtcNow
                    });
                }

                handler.Handle(new DashboardRequeueAllErrorMessagesCommand());

                using (var db = connectionManager.GetDatabase())
                {
                    var errorCol = db.Database.GetCollection<MetaDataErrorsTable>(tableNameHelper.MetaDataErrorsName);
                    Assert.AreEqual(0, errorCol.Count());
                }
            }
            finally
            {
                connectionManager.Dispose();
                TryDeleteFile(dbPath);
            }
        }

        [TestMethod]
        public void Handle_WithErrorRecords_StatusTableEnabled_UpdatesStatusTable()
        {
            var (handler, connectionManager, tableNameHelper, dbPath) = CreateHandlerWithDb(enableStatusTable: true);
            try
            {
                using (var db = connectionManager.GetDatabase())
                {
                    var errorCol = db.Database.GetCollection<MetaDataErrorsTable>(tableNameHelper.MetaDataErrorsName);
                    errorCol.Insert(new MetaDataErrorsTable
                    {
                        QueueId = 1,
                        CorrelationId = Guid.NewGuid(),
                        Status = QueueStatuses.Error,
                        QueuedDateTime = DateTime.UtcNow,
                        LastException = "err",
                        LastExceptionDate = DateTime.UtcNow
                    });

                    var metaCol = db.Database.GetCollection<MetaDataTable>(tableNameHelper.MetaDataName);
                    metaCol.Insert(new MetaDataTable
                    {
                        QueueId = 1,
                        CorrelationId = Guid.NewGuid(),
                        Status = QueueStatuses.Error,
                        QueuedDateTime = DateTime.UtcNow
                    });

                    var statusCol = db.Database.GetCollection<StatusTable>(tableNameHelper.StatusName);
                    statusCol.Insert(new StatusTable
                    {
                        QueueId = 1,
                        CorrelationId = Guid.NewGuid(),
                        Status = QueueStatuses.Error
                    });
                }

                var result = handler.Handle(new DashboardRequeueAllErrorMessagesCommand());
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
        public void Handle_WithErrorRecords_StatusTableDisabled_DoesNotTouchStatusTable()
        {
            var (handler, connectionManager, tableNameHelper, dbPath) = CreateHandlerWithDb(enableStatusTable: false);
            try
            {
                using (var db = connectionManager.GetDatabase())
                {
                    var errorCol = db.Database.GetCollection<MetaDataErrorsTable>(tableNameHelper.MetaDataErrorsName);
                    errorCol.Insert(new MetaDataErrorsTable
                    {
                        QueueId = 1,
                        CorrelationId = Guid.NewGuid(),
                        Status = QueueStatuses.Error,
                        QueuedDateTime = DateTime.UtcNow,
                        LastException = "err",
                        LastExceptionDate = DateTime.UtcNow
                    });

                    var statusCol = db.Database.GetCollection<StatusTable>(tableNameHelper.StatusName);
                    statusCol.Insert(new StatusTable
                    {
                        QueueId = 1,
                        CorrelationId = Guid.NewGuid(),
                        Status = QueueStatuses.Error
                    });
                }

                handler.Handle(new DashboardRequeueAllErrorMessagesCommand());

                using (var db = connectionManager.GetDatabase())
                {
                    var statusCol = db.Database.GetCollection<StatusTable>(tableNameHelper.StatusName);
                    var statusRecord = statusCol.Query().Where(x => x.QueueId == 1).FirstOrDefault();
                    Assert.IsNotNull(statusRecord);
                    Assert.AreEqual(QueueStatuses.Error, statusRecord.Status);
                }
            }
            finally
            {
                connectionManager.Dispose();
                TryDeleteFile(dbPath);
            }
        }

        [TestMethod]
        public void Handle_MultipleErrorRecords_RequeuesAll()
        {
            var (handler, connectionManager, tableNameHelper, dbPath) = CreateHandlerWithDb();
            try
            {
                using (var db = connectionManager.GetDatabase())
                {
                    var errorCol = db.Database.GetCollection<MetaDataErrorsTable>(tableNameHelper.MetaDataErrorsName);
                    for (int i = 1; i <= 3; i++)
                    {
                        errorCol.Insert(new MetaDataErrorsTable
                        {
                            QueueId = i,
                            CorrelationId = Guid.NewGuid(),
                            Status = QueueStatuses.Error,
                            QueuedDateTime = DateTime.UtcNow,
                            LastException = $"Error {i}",
                            LastExceptionDate = DateTime.UtcNow
                        });
                    }
                }

                var result = handler.Handle(new DashboardRequeueAllErrorMessagesCommand());
                Assert.AreEqual(3, result);
            }
            finally
            {
                connectionManager.Dispose();
                TryDeleteFile(dbPath);
            }
        }

        private static (DashboardRequeueAllErrorMessagesCommandHandler handler, LiteDbConnectionManager connectionManager, TableNameHelper tableNameHelper)
            CreateHandler()
        {
            var optionsFactory = Substitute.For<ILiteDbMessageQueueTransportOptionsFactory>();
            optionsFactory.Create().Returns(new LiteDbMessageQueueTransportOptions());
            var connectionManager = CreateConnectionManager();
            var tableNameHelper = CreateTableNameHelper();
            var handler = new DashboardRequeueAllErrorMessagesCommandHandler(optionsFactory, connectionManager, tableNameHelper);
            return (handler, connectionManager, tableNameHelper);
        }

        private static (DashboardRequeueAllErrorMessagesCommandHandler handler, LiteDbConnectionManager connectionManager, TableNameHelper tableNameHelper, string dbPath)
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

            var handler = new DashboardRequeueAllErrorMessagesCommandHandler(optionsFactory, connectionManager, tableNameHelper);
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
