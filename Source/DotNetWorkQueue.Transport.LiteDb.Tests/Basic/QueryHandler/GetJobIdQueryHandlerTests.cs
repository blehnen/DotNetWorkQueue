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
using DotNetWorkQueue.Transport.LiteDb.Schema;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.LiteDb.Tests.Basic.QueryHandler
{
    [TestClass]
    public class GetJobIdQueryHandlerTests
    {
        private const string QueueName = "testQueue";
        // LiteDb in-memory connection string; unique db name per test avoids cross-test leaks.
        private static string MemoryConnectionString() =>
            $"Filename=:memory:{Guid.NewGuid():N};Mode=Memory";

        [TestMethod]
        public void Create_Default()
        {
            var connInfo = Substitute.For<IConnectionInformation>();
            connInfo.QueueName.Returns(QueueName);
            connInfo.ConnectionString.Returns(MemoryConnectionString());
            var scope = Substitute.For<ICreationScope>();
            using var connMgr = new LiteDbConnectionManager(connInfo, scope);
            var tableNameHelper = new TableNameHelper(connInfo);

            var handler = new GetJobIdQueryHandler(connMgr, tableNameHelper);
            Assert.IsNotNull(handler);
        }

        [TestMethod]
        public void Create_Null_ConnectionInformation_Throws()
        {
            var connInfo = Substitute.For<IConnectionInformation>();
            connInfo.QueueName.Returns(QueueName);
            var tableNameHelper = new TableNameHelper(connInfo);

            Assert.ThrowsExactly<ArgumentNullException>(
                () => new GetJobIdQueryHandler(null, tableNameHelper));
        }

        [TestMethod]
        public void Create_Null_TableNameHelper_Throws()
        {
            var connInfo = Substitute.For<IConnectionInformation>();
            connInfo.QueueName.Returns(QueueName);
            connInfo.ConnectionString.Returns(MemoryConnectionString());
            var scope = Substitute.For<ICreationScope>();
            using var connMgr = new LiteDbConnectionManager(connInfo, scope);

            Assert.ThrowsExactly<ArgumentNullException>(
                () => new GetJobIdQueryHandler(connMgr, null));
        }

        [TestMethod]
        public void Handle_JobExists_ReturnsJobId()
        {
            var (handler, connMgr, tnh) = CreateHandler();
            using (connMgr)
            {
                // Seed a StatusTable row representing a queued job
                using (var db = connMgr.GetDatabase())
                {
                    var col = db.Database.GetCollection<StatusTable>(tnh.StatusName);
                    col.Insert(new StatusTable
                    {
                        QueueId = 42,
                        JobName = "myJob",
                        Status = QueueStatuses.Waiting
                    });
                }

                var query = new GetJobIdQuery<int>("myJob");
                var result = handler.Handle(query);

                result.Should().Be(42);
            }
        }

        [TestMethod]
        public void Handle_JobDoesNotExist_ReturnsZero()
        {
            var (handler, connMgr, _) = CreateHandler();
            using (connMgr)
            {
                // No records inserted
                var query = new GetJobIdQuery<int>("missingJob");
                var result = handler.Handle(query);

                result.Should().Be(0);
            }
        }

        [TestMethod]
        public void Handle_MultipleJobs_ReturnsMatchingId()
        {
            var (handler, connMgr, tnh) = CreateHandler();
            using (connMgr)
            {
                using (var db = connMgr.GetDatabase())
                {
                    var col = db.Database.GetCollection<StatusTable>(tnh.StatusName);
                    col.Insert(new StatusTable
                    {
                        QueueId = 1,
                        JobName = "jobA",
                        Status = QueueStatuses.Waiting
                    });
                    col.Insert(new StatusTable
                    {
                        QueueId = 2,
                        JobName = "jobB",
                        Status = QueueStatuses.Processing
                    });
                    col.Insert(new StatusTable
                    {
                        QueueId = 3,
                        JobName = "jobC",
                        Status = QueueStatuses.Waiting
                    });
                }

                var query = new GetJobIdQuery<int>("jobB");
                var result = handler.Handle(query);

                result.Should().Be(2);
            }
        }

        [TestMethod]
        public void Handle_NonMatchingJobName_ReturnsZero()
        {
            var (handler, connMgr, tnh) = CreateHandler();
            using (connMgr)
            {
                using (var db = connMgr.GetDatabase())
                {
                    var col = db.Database.GetCollection<StatusTable>(tnh.StatusName);
                    col.Insert(new StatusTable
                    {
                        QueueId = 10,
                        JobName = "someOtherJob",
                        Status = QueueStatuses.Waiting
                    });
                }

                var query = new GetJobIdQuery<int>("notThere");
                var result = handler.Handle(query);

                result.Should().Be(0);
            }
        }

        private static (GetJobIdQueryHandler handler, LiteDbConnectionManager connMgr, TableNameHelper tnh)
            CreateHandler()
        {
            var connInfo = Substitute.For<IConnectionInformation>();
            connInfo.QueueName.Returns(QueueName);
            connInfo.ConnectionString.Returns(MemoryConnectionString());
            var scope = Substitute.For<ICreationScope>();
            var connMgr = new LiteDbConnectionManager(connInfo, scope);
            var tnh = new TableNameHelper(connInfo);
            var handler = new GetJobIdQueryHandler(connMgr, tnh);
            return (handler, connMgr, tnh);
        }
    }
}
