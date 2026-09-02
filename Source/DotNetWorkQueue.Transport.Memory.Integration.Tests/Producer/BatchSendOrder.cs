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
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests.Producer
{
    /// <summary>
    /// The batch send returns one result per message, in the order the caller supplied them.
    /// </summary>
    /// <remarks>
    /// Callers match a generated id back to the message it belongs to by position, and every other
    /// transport returns results in input order. The Memory transport used to fan the batch out
    /// with <c>Parallel.ForEach</c> into a <c>ConcurrentBag</c>, so its results came back in
    /// whatever order the threads happened to finish. This pins the contract; it fails against
    /// that shape.
    /// </remarks>
    [TestClass]
    public class BatchSendOrder
    {
        [TestMethod]
        [DataRow(100)]
        [DataRow(1000)]
        public void Returns_Results_In_Caller_Order(int messageCount)
        {
            Run(messageCount, (producer, batch) => producer.Send(batch).ToList());
        }

        [TestMethod]
        [DataRow(100)]
        [DataRow(1000)]
        public void SendAsync_Returns_Results_In_Caller_Order(int messageCount)
        {
            //the async batch filled the same ConcurrentBag from a sequential loop, so it lost the
            //order too - for a reason that was even less visible
            Run(messageCount, (producer, batch) =>
                Task.Run(async () => (await producer.SendAsync(batch)).ToList()).GetAwaiter().GetResult());
        }

        private static void Run(int messageCount,
            Func<IProducerQueue<FakeMessage>, List<QueueMessage<FakeMessage, IAdditionalMessageData>>,
                List<IQueueOutputMessage>> send)
        {
            using var connectionInfo = new IntegrationConnectionInfo();
            var queueConnection = new QueueConnection(GenerateQueueName.Create(), connectionInfo.ConnectionString);

            using (var creation = new QueueCreationContainer<MemoryMessageQueueInit>())
            {
                using var creator = creation.GetQueueCreation<MessageQueueCreation>(queueConnection);
                var created = creator.CreateQueue();
                Assert.IsTrue(created.Success, created.ErrorMessage);
            }

            using var container = new QueueContainer<MemoryMessageQueueInit>();
            using var producer = container.CreateProducer<FakeMessage>(queueConnection);

            //a distinct, known correlation id per message is what makes the order checkable: the
            //generated message ids are GUIDs, so they carry no ordering of their own
            var batch = new List<QueueMessage<FakeMessage, IAdditionalMessageData>>(messageCount);
            var sent = new List<Guid>(messageCount);
            for (var i = 0; i < messageCount; i++)
            {
                var id = Guid.NewGuid();
                sent.Add(id);
                var data = new AdditionalMessageData { CorrelationId = new MessageCorrelationId(id) };
                batch.Add(new QueueMessage<FakeMessage, IAdditionalMessageData>(new FakeMessage(), data));
            }

            var results = send(producer, batch);

            Assert.HasCount(messageCount, results);
            Assert.IsFalse(results.Any(r => r.HasError),
                results.FirstOrDefault(r => r.HasError)?.SendingException?.ToString() ?? "no error");

            var returned = results.Select(r => (Guid)r.SentMessage.CorrelationId.Id.Value).ToList();
            CollectionAssert.AreEqual(sent, returned,
                "results came back in a different order than the messages were sent");

            Assert.IsFalse(results.Any(r => (Guid)r.SentMessage.MessageId.Id.Value == Guid.Empty),
                "every message should come back with a real id");
            Assert.AreEqual(messageCount,
                results.Select(r => (Guid)r.SentMessage.MessageId.Id.Value).Distinct().Count(),
                "ids should be unique");
        }
    }
}
