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
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests.Serialization
{
    /// <summary>
    /// The serializer marker travels in the message headers, so it only really works if the
    /// transport carries it. These go through a real producer and a real consumer rather than
    /// calling the serializers directly.
    /// </summary>
    [TestClass]
    public class SerializerMarkerTests
    {
        [TestMethod]
        public void A_Consumer_Reads_A_Message_Written_By_A_Different_Serializer()
        {
            //the migration case: producers still write Newtonsoft, the consumer has been switched
            //to System.Text.Json and has to keep reading what is already in the queue
            var connection = new QueueConnection(GenerateQueueName.Create(), "memory");
            CreateQueue(connection);

            Send(connection, register: null); //default: Newtonsoft

            var received = Consume(connection, register: container =>
            {
                var binder = new DenyListSerializationBinder();
                container.Register<ISerializer>(() => new SystemTextJsonSerializer(binder), LifeStyles.Singleton);
                container.Register<ISerializerResolver>(() =>
                {
                    var resolver = new SerializerResolver(new SystemTextJsonSerializer(binder));
                    resolver.Add(new JsonSerializer(binder));
                    return resolver;
                }, LifeStyles.Singleton);
            });

            Assert.AreEqual(Payload, received);
        }

        [TestMethod]
        public void A_Message_Written_By_SystemTextJson_Round_Trips()
        {
            var connection = new QueueConnection(GenerateQueueName.Create(), "memory");
            CreateQueue(connection);

            Action<IContainer> useStj = container =>
            {
                var binder = new DenyListSerializationBinder();
                container.Register<ISerializer>(() => new SystemTextJsonSerializer(binder), LifeStyles.Singleton);
            };

            Send(connection, useStj);

            Assert.AreEqual(Payload, Consume(connection, useStj));
        }

        [TestMethod]
        public void A_Producer_Stamps_The_Serializer_That_Wrote_The_Body()
        {
            //if the transport drops the header, everything above still passes by accident because
            //the fallback happens to be right - so assert the marker actually survives the trip
            var connection = new QueueConnection(GenerateQueueName.Create(), "memory");
            CreateQueue(connection);

            Send(connection, register: null);

            string stamped = null;
            Consume(connection, register: null, inspect: headers =>
                stamped = headers.TryGetValue("Queue-SerializerId", out var value) ? value as string : null);

            Assert.AreEqual(JsonSerializer.Id, stamped);
        }

        private const string Payload = "a body that has to survive the round trip";

        private static void CreateQueue(QueueConnection connection)
        {
            using var creation = new QueueCreationContainer<MemoryMessageQueueInit>();
            using var creator = creation.GetQueueCreation<MessageQueueCreation>(connection);
            creator.CreateQueue();
        }

        private static void Send(QueueConnection connection, Action<IContainer> register)
        {
            using var container = register == null
                ? new QueueContainer<MemoryMessageQueueInit>()
                : new QueueContainer<MemoryMessageQueueInit>(register);
            using var producer = container.CreateProducer<SerializerTestMessage>(connection);

            var result = producer.Send(new SerializerTestMessage { Body = Payload });

            Assert.IsFalse(result.HasError, result.SendingException?.ToString());
        }

        private static string Consume(QueueConnection connection, Action<IContainer> register,
            Action<IReadOnlyDictionary<string, object>> inspect = null)
        {
            using var container = register == null
                ? new QueueContainer<MemoryMessageQueueInit>()
                : new QueueContainer<MemoryMessageQueueInit>(register);

            string body = null;
            var done = new ManualResetEventSlim(false);
            using (var consumer = container.CreateConsumer(connection))
            {
                consumer.Configuration.Worker.WorkerCount = 1;
                consumer.Start<SerializerTestMessage>((message, notifications) =>
                {
                    body = message.Body.Body;
                    inspect?.Invoke(message.Headers);
                    done.Set();
                }, new ConsumerQueueNotifications());

                Assert.IsTrue(done.Wait(TimeSpan.FromSeconds(30)), "the message was never consumed");
            }

            return body;
        }

        public class SerializerTestMessage
        {
            public string Body { get; set; }
        }
    }
}
