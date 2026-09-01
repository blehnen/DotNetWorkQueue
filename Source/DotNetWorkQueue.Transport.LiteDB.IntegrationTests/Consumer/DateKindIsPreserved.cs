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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using LiteDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests.Consumer
{
    /// <summary>
    /// The de-queue compares stored times with <c>UtcNow</c> directly, which is only correct while
    /// LiteDB hands them back as UTC.
    /// </summary>
    /// <remarks>
    /// LiteDB returns a plain <see cref="DateTime"/> property as <see cref="DateTimeKind.Local"/>
    /// in other shapes. Comparing one of those with <c>UtcNow</c> compares raw ticks without
    /// applying the offset, so a message deferred an hour into the future reads as ready - it would
    /// be released early, and every other test here would still pass. This pins the assumption.
    /// </remarks>
    [TestClass]
    public class DateKindIsPreserved
    {
        [TestMethod]
        public void Stored_Times_Come_Back_As_Utc()
        {
            using var connectionInfo = new IntegrationConnectionInfo(
                IntegrationConnectionInfo.ConnectionTypes.Direct);
            var queueConnection = new QueueConnection(GenerateQueueName.Create(), connectionInfo.ConnectionString);

            using (var creation = new QueueCreationContainer<LiteDbMessageQueueInit>())
            {
                using var creator = creation.GetQueueCreation<LiteDbMessageQueueCreation>(queueConnection);
                creator.Options.EnableDelayedProcessing = true;
                Assert.IsTrue(creator.CreateQueue().Success);
            }

            using (var container = new QueueContainer<LiteDbMessageQueueInit>())
            {
                using var producer = container.CreateProducer<DequeueEligibility.OrderedMessage>(queueConnection);
                var data = new AdditionalMessageData();
                data.SetDelay(TimeSpan.FromHours(1));
                Assert.IsFalse(producer.Send(
                    new DequeueEligibility.OrderedMessage { Body = "deferred" }, data).HasError);
            }

            var helper = new TableNameHelper(new LiteDbConnectionInformation(queueConnection));
            using var db = new LiteDatabase(connectionInfo.ConnectionString);
            var row = db.GetCollection<Schema.MetaDataTable>(helper.MetaDataName).FindAll().First();

            Assert.IsTrue(row.QueueProcessTime.HasValue, "the delay should have been stored");
            Assert.AreEqual(DateTimeKind.Utc, row.QueueProcessTime.Value.Kind,
                "the de-queue compares this with UtcNow directly; a Local value would release " +
                "delayed messages early");

            //and the comparison the de-queue makes gives the right answer
            Assert.IsTrue(row.QueueProcessTime.Value >= DateTime.UtcNow,
                "a message deferred an hour ahead should compare as not yet due");
        }
    }
}
