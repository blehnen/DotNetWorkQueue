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
using System.Linq;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.Basic.Command;
using DotNetWorkQueue.Transport.LiteDb.Basic.CommandHandler;
using DotNetWorkQueue.Transport.LiteDb.Schema;
using LiteDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.LiteDb.Tests.Basic.CommandHandler
{
    [TestClass]
    public class SetJobLastKnownEventCommandHandlerTests
    {
        private const string QueueName = "testQueue";

        [TestMethod]
        public void Create_Default()
        {
            var tableNameHelper = CreateTableNameHelper();
            var handler = new SetJobLastKnownEventCommandHandler(tableNameHelper);
            Assert.IsNotNull(handler);
        }

        [TestMethod]
        public void Handle_NewJob_InsertsRecord()
        {
            var tableNameHelper = CreateTableNameHelper();
            var handler = new SetJobLastKnownEventCommandHandler(tableNameHelper);

            using var db = new LiteDatabase("Filename=:memory:");

            var scheduledTime = new DateTimeOffset(2026, 4, 12, 10, 0, 0, TimeSpan.Zero);
            var eventTime = new DateTimeOffset(2026, 4, 12, 10, 0, 5, TimeSpan.Zero);

            var command = new SetJobLastKnownEventCommand("TestJob", eventTime, scheduledTime, db);
            handler.Handle(command);

            var col = db.GetCollection<JobsTable>(tableNameHelper.JobTableName);
            var all = col.FindAll().ToList();

            Assert.HasCount(1, all);
            Assert.AreEqual("TestJob", all[0].JobName);
            Assert.AreEqual(scheduledTime, all[0].JobScheduledTime);
            Assert.AreEqual(eventTime, all[0].JobEventTime);
        }

        [TestMethod]
        public void Handle_ExistingJob_UpdatesTimestamps()
        {
            var tableNameHelper = CreateTableNameHelper();
            var handler = new SetJobLastKnownEventCommandHandler(tableNameHelper);

            using var db = new LiteDatabase("Filename=:memory:");

            var initialScheduled = new DateTimeOffset(2026, 4, 1, 9, 0, 0, TimeSpan.Zero);
            var initialEvent = new DateTimeOffset(2026, 4, 1, 9, 0, 5, TimeSpan.Zero);

            var col = db.GetCollection<JobsTable>(tableNameHelper.JobTableName);
            col.Insert(new JobsTable
            {
                JobName = "ExistingJob",
                JobScheduledTime = initialScheduled,
                JobEventTime = initialEvent
            });

            var newScheduled = new DateTimeOffset(2026, 4, 12, 12, 0, 0, TimeSpan.Zero);
            var newEvent = new DateTimeOffset(2026, 4, 12, 12, 0, 7, TimeSpan.Zero);

            var command = new SetJobLastKnownEventCommand("ExistingJob", newEvent, newScheduled, db);
            handler.Handle(command);

            var all = col.FindAll().ToList();
            Assert.HasCount(1, all, "existing job should be updated, not duplicated");
            Assert.AreEqual("ExistingJob", all[0].JobName);
            Assert.AreEqual(newScheduled, all[0].JobScheduledTime);
            Assert.AreEqual(newEvent, all[0].JobEventTime);
        }

        [TestMethod]
        public void Handle_DifferentJobName_InsertsNewRecord()
        {
            var tableNameHelper = CreateTableNameHelper();
            var handler = new SetJobLastKnownEventCommandHandler(tableNameHelper);

            using var db = new LiteDatabase("Filename=:memory:");

            var firstScheduled = new DateTimeOffset(2026, 4, 1, 9, 0, 0, TimeSpan.Zero);
            var firstEvent = new DateTimeOffset(2026, 4, 1, 9, 0, 5, TimeSpan.Zero);

            var col = db.GetCollection<JobsTable>(tableNameHelper.JobTableName);
            col.Insert(new JobsTable
            {
                JobName = "FirstJob",
                JobScheduledTime = firstScheduled,
                JobEventTime = firstEvent
            });

            var secondScheduled = new DateTimeOffset(2026, 4, 12, 10, 0, 0, TimeSpan.Zero);
            var secondEvent = new DateTimeOffset(2026, 4, 12, 10, 0, 3, TimeSpan.Zero);

            var command = new SetJobLastKnownEventCommand("SecondJob", secondEvent, secondScheduled, db);
            handler.Handle(command);

            var all = col.FindAll().OrderBy(j => j.JobName).ToList();
            Assert.HasCount(2, all);

            var first = all.Single(j => j.JobName == "FirstJob");
            Assert.AreEqual(firstScheduled, first.JobScheduledTime);
            Assert.AreEqual(firstEvent, first.JobEventTime);

            var second = all.Single(j => j.JobName == "SecondJob");
            Assert.AreEqual(secondScheduled, second.JobScheduledTime);
            Assert.AreEqual(secondEvent, second.JobEventTime);
        }

        private static TableNameHelper CreateTableNameHelper()
        {
            var connInfo = Substitute.For<IConnectionInformation>();
            connInfo.QueueName.Returns(QueueName);
            return new TableNameHelper(connInfo);
        }
    }
}
