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
using System.Collections.Generic;
using System.Linq;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests.Producer
{
    /// <summary>
    /// Covers what the shared producer tests do not: that the batch path returns an id for every
    /// message, in the caller's order, and that the ids are real.
    /// </summary>
    /// <remarks>
    /// Ordering matters because a caller correlates the returned ids back to the messages it sent
    /// by position. The batch path inserts sequentially inside one transaction, so the order is
    /// natural — this pins it so a future bulk-insert rewrite cannot quietly lose it.
    /// </remarks>
    [TestClass]
    public class BatchSendResults
    {
        [TestMethod]
        [DataRow(50, IntegrationConnectionInfo.ConnectionTypes.Direct)]
        [DataRow(50, IntegrationConnectionInfo.ConnectionTypes.Memory)]
        [DataRow(10, IntegrationConnectionInfo.ConnectionTypes.Shared)]
        public void Returns_An_Id_For_Every_Message_In_Caller_Order(
            int messageCount, IntegrationConnectionInfo.ConnectionTypes connectionType)
        {
            using var connectionInfo = new IntegrationConnectionInfo(connectionType);
            var queueConnection = new QueueConnection(GenerateQueueName.Create(), connectionInfo.ConnectionString);

            using (var creation = new QueueCreationContainer<LiteDbMessageQueueInit>())
            {
                using var creator = creation.GetQueueCreation<LiteDbMessageQueueCreation>(queueConnection);
                var created = creator.CreateQueue();
                Assert.IsTrue(created.Success, created.ErrorMessage);
            }

            using var container = new QueueContainer<LiteDbMessageQueueInit>();
            using var producer = container.CreateProducer<FakeMessage>(queueConnection);

            var batch = Enumerable.Range(0, messageCount).Select(_ => new FakeMessage()).ToList();

            var results = producer.Send(batch).ToList();

            Assert.HasCount(messageCount, results);
            Assert.IsFalse(results.Any(r => r.HasError),
                results.FirstOrDefault(r => r.HasError)?.SendingException?.ToString() ?? "no error");

            var ids = results.Select(r => (int)r.SentMessage.MessageId.Id.Value).ToList();
            Assert.IsFalse(ids.Any(id => id <= 0), "every message should come back with a real id");
            Assert.AreEqual(messageCount, ids.Distinct().Count(), "ids should be unique");

            //inserted sequentially in one transaction, so the caller's order is the id order
            CollectionAssert.AreEqual(ids.OrderBy(id => id).ToList(), ids,
                "ids came back in a different order than the messages were sent");
        }

        [TestMethod]
        public void An_Empty_Batch_Sends_Nothing_And_Succeeds()
        {
            using var connectionInfo = new IntegrationConnectionInfo(IntegrationConnectionInfo.ConnectionTypes.Direct);
            var queueConnection = new QueueConnection(GenerateQueueName.Create(), connectionInfo.ConnectionString);

            //the batch handler also returns an empty result when the database is missing, so
            //without this the assertion below would pass for a queue that was never created
            using (var creation = new QueueCreationContainer<LiteDbMessageQueueInit>())
            {
                using var creator = creation.GetQueueCreation<LiteDbMessageQueueCreation>(queueConnection);
                var created = creator.CreateQueue();
                Assert.IsTrue(created.Success, created.ErrorMessage);
            }

            using var container = new QueueContainer<LiteDbMessageQueueInit>();
            using var producer = container.CreateProducer<FakeMessage>(queueConnection);

            var results = producer.Send(new List<FakeMessage>()).ToList();

            Assert.IsEmpty(results);
        }
    }
}
