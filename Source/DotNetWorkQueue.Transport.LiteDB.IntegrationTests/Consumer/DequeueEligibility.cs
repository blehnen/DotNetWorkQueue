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
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests.Consumer
{
    /// <summary>
    /// The de-queue used to hand its eligibility tests to LiteDB as <c>Where</c> clauses; they now
    /// run in memory over an ordered window. These cover the behaviours that moved.
    /// </summary>
    [TestClass]
    public class DequeueEligibility
    {
        [TestMethod]
        public void Messages_Are_Consumed_In_The_Order_They_Were_Sent()
        {
            //the walk is ordered by primary key, which is insertion order - if that ever stopped
            //being true, FIFO would break silently
            using var connectionInfo = new IntegrationConnectionInfo(
                IntegrationConnectionInfo.ConnectionTypes.Direct);
            var queueConnection = Create(connectionInfo, _ => { });

            var sent = Enumerable.Range(0, 25).Select(i => $"message-{i:00}").ToList();
            using (var container = new QueueContainer<LiteDbMessageQueueInit>())
            {
                using var producer = container.CreateProducer<OrderedMessage>(queueConnection);
                foreach (var body in sent)
                    Assert.IsFalse(producer.Send(new OrderedMessage { Body = body }).HasError);
            }

            var received = Consume<OrderedMessage>(queueConnection, sent.Count, m => m.Body);

            CollectionAssert.AreEqual(sent, received, "messages came back in a different order");
        }

        [TestMethod]
        public void A_Delayed_Message_Is_Not_Consumed_Before_It_Is_Due()
        {
            //this is the comparison that has to convert before comparing - LiteDB hands the value
            //back as Local, and comparing that to UtcNow without converting reads a message
            //deferred into the future as ready
            using var connectionInfo = new IntegrationConnectionInfo(
                IntegrationConnectionInfo.ConnectionTypes.Direct);
            var queueConnection = Create(connectionInfo, o => o.EnableDelayedProcessing = true);

            using (var container = new QueueContainer<LiteDbMessageQueueInit>())
            {
                using var producer = container.CreateProducer<OrderedMessage>(queueConnection);

                var deferred = new AdditionalMessageData();
                deferred.SetDelay(TimeSpan.FromHours(1));
                Assert.IsFalse(producer.Send(new OrderedMessage { Body = "not yet" }, deferred).HasError);

                //a control sent after it: consuming this proves the consumer ran and had time to
                //reach the deferred message, so "the deferred one did not arrive" means something
                Assert.IsFalse(producer.Send(new OrderedMessage { Body = "ready" }).HasError);
            }

            var received = Consume<OrderedMessage>(queueConnection, 1, m => m.Body);

            CollectionAssert.AreEqual(new List<string> { "ready" }, received,
                "the control should arrive and the message deferred an hour should not");
        }

        [TestMethod]
        public void An_Expired_Message_Is_Not_Consumed()
        {
            using var connectionInfo = new IntegrationConnectionInfo(
                IntegrationConnectionInfo.ConnectionTypes.Direct);
            var queueConnection = Create(connectionInfo, o => o.EnableMessageExpiration = true);

            using (var container = new QueueContainer<LiteDbMessageQueueInit>())
            {
                using var producer = container.CreateProducer<OrderedMessage>(queueConnection);

                var expires = new AdditionalMessageData();
                expires.SetExpiration(TimeSpan.FromMilliseconds(1));
                Assert.IsFalse(producer.Send(new OrderedMessage { Body = "stale" }, expires).HasError);

                Thread.Sleep(50);

                //same control: without it, "nothing arrived" could just mean the consumer was slow
                Assert.IsFalse(producer.Send(new OrderedMessage { Body = "fresh" }).HasError);
            }

            var received = Consume<OrderedMessage>(queueConnection, 1, m => m.Body);

            CollectionAssert.AreEqual(new List<string> { "fresh" }, received,
                "the control should arrive and the expired message should not");
        }

        [TestMethod]
        public void A_Message_Beyond_One_Polls_Reach_Is_Still_Found()
        {
            //a poll examines a bounded number of rows and resumes where it stopped, so that a queue
            //of deferred messages cannot make every poll read the whole collection. This puts the
            //only ready message past that bound: it is reachable solely by the resume working, and
            //nothing else in the suite gets near it.
            const int BeyondOnePoll = 1200;

            using var connectionInfo = new IntegrationConnectionInfo(
                IntegrationConnectionInfo.ConnectionTypes.Direct);
            var queueConnection = Create(connectionInfo, o => o.EnableDelayedProcessing = true);

            using (var container = new QueueContainer<LiteDbMessageQueueInit>())
            {
                using var producer = container.CreateProducer<OrderedMessage>(queueConnection);

                var deferred = new List<QueueMessage<OrderedMessage, IAdditionalMessageData>>(BeyondOnePoll);
                for (var i = 0; i < BeyondOnePoll; i++)
                {
                    var data = new AdditionalMessageData();
                    data.SetDelay(TimeSpan.FromHours(1));
                    deferred.Add(new QueueMessage<OrderedMessage, IAdditionalMessageData>(
                        new OrderedMessage { Body = $"deferred-{i}" }, data));
                }

                Assert.IsFalse(producer.Send(deferred).HasErrors);
                Assert.IsFalse(producer.Send(new OrderedMessage { Body = "reachable" }).HasError);
            }

            var received = Consume<OrderedMessage>(queueConnection, 1, m => m.Body);

            CollectionAssert.AreEqual(new List<string> { "reachable" }, received,
                "the ready message sits past what a single poll examines; only the resume finds it");
        }

        private static QueueConnection Create(IntegrationConnectionInfo connectionInfo,
            Action<LiteDbMessageQueueTransportOptions> options)
        {
            var queueConnection = new QueueConnection(GenerateQueueName.Create(), connectionInfo.ConnectionString);
            using var creation = new QueueCreationContainer<LiteDbMessageQueueInit>();
            using var creator = creation.GetQueueCreation<LiteDbMessageQueueCreation>(queueConnection);
            options(creator.Options);
            var created = creator.CreateQueue();
            Assert.IsTrue(created.Success, created.ErrorMessage);
            return queueConnection;
        }

        /// <summary>Consumes up to <paramref name="expected"/> messages, or gives up.</summary>
        private static List<string> Consume<T>(QueueConnection queueConnection, int expected,
            Func<T, string> select, TimeSpan? limit = null)
            where T : class
        {
            var received = new List<string>();
            var done = new ManualResetEventSlim(false);

            using var container = new QueueContainer<LiteDbMessageQueueInit>();
            using var consumer = container.CreateConsumer(queueConnection);
            consumer.Configuration.Worker.WorkerCount = 1;
            consumer.Start<T>((message, notifications) =>
            {
                lock (received)
                {
                    received.Add(select(message.Body));
                    if (received.Count >= expected) done.Set();
                }
            }, new ConsumerQueueNotifications());

            done.Wait(limit ?? TimeSpan.FromSeconds(30));
            lock (received) return received.ToList();
        }

        public class OrderedMessage
        {
            public string Body { get; set; }
        }
    }
}
