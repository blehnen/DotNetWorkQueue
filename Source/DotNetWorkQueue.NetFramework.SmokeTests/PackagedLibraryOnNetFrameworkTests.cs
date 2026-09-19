using System;
using System.IO;
using System.Threading;
using DotNetWorkQueue;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Interceptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LiteDbBasic = DotNetWorkQueue.Transport.LiteDb.Basic;
using MemoryBasic = DotNetWorkQueue.Transport.Memory.Basic;
using SqliteBasic = DotNetWorkQueue.Transport.SQLite.Basic;

namespace DotNetWorkQueue.NetFramework.SmokeTests
{
    /// <summary>
    /// Runs the shipped netstandard2.0 packages on .NET Framework (GitHub #392).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Everything else in this repository is tested on net10.0, where the framework supplies the
    /// APIs the netstandard2.0 build has to polyfill - so none of that code is ever executed. This
    /// project exists to execute it: the queue-name regular expressions built at run time rather
    /// than generated, the ADO.NET asynchronous transaction shims, the interval timer standing in
    /// for PeriodicTimer, and the AES-GCM implementation that framework has no equivalent for.
    /// </para>
    /// <para>
    /// Breadth is not the point; a second copy of the main suite would cost a great deal and catch
    /// little. What matters is that the packaged library loads, its container verifies, and a
    /// message survives a round trip on this framework.
    /// </para>
    /// </remarks>
    [TestClass]
    public class PackagedLibraryOnNetFrameworkTests
    {
        private static readonly TimeSpan ReceiveWindow = TimeSpan.FromSeconds(30);

        [TestMethod]
        public void A_Message_Round_Trips_Through_The_Memory_Transport()
        {
            var connection = new QueueConnection(NewQueueName(), "memory");

            using (var creation = new QueueCreationContainer<MemoryBasic.MemoryMessageQueueInit>())
            using (var creator = creation.GetQueueCreation<MemoryBasic.MessageQueueCreation>(connection))
            {
                var created = creator.CreateQueue();
                Assert.IsTrue(created.Success, created.Status.ToString());
            }

            RoundTrip<MemoryBasic.MemoryMessageQueueInit>(connection, "memory round trip");
        }

        [TestMethod]
        public void A_Message_Round_Trips_Through_Sqlite()
        {
            //SQLite is the transport worth running here: it drives the relational command handlers,
            //and with them the asynchronous transaction members that netstandard2.0 has no
            //framework implementation of. Nothing on net10.0 exercises those shims.
            var file = NewDatabaseFile(".sqlite");
            try
            {
                var connection = new QueueConnection(NewQueueName(), $"Data Source={file};Version=3;");

                using (var creation = new QueueCreationContainer<SqliteBasic.SqLiteMessageQueueInit>())
                using (var creator = creation.GetQueueCreation<SqliteBasic.SqLiteMessageQueueCreation>(connection))
                {
                    var created = creator.CreateQueue();
                    Assert.IsTrue(created.Success, created.Status.ToString());
                }

                RoundTrip<SqliteBasic.SqLiteMessageQueueInit>(connection, "sqlite round trip");
            }
            finally
            {
                Delete(file);
            }
        }

        [TestMethod]
        public void A_Queue_Creates_And_Removes_On_LiteDb()
        {
            var file = NewDatabaseFile(".litedb");
            try
            {
                var connection = new QueueConnection(NewQueueName(), $"Filename={file};Connection=direct;");

                using (var creation = new QueueCreationContainer<LiteDbBasic.LiteDbMessageQueueInit>())
                using (var creator = creation.GetQueueCreation<LiteDbBasic.LiteDbMessageQueueCreation>(connection))
                {
                    var created = creator.CreateQueue();
                    Assert.IsTrue(created.Success, created.Status.ToString());

                    var removed = creator.RemoveQueue();
                    Assert.IsTrue(removed.Success, removed.Status.ToString());
                }
            }
            finally
            {
                Delete(file);
            }
        }

        [TestMethod]
        public void A_Queue_Name_The_Transport_Cannot_Accept_Is_Refused()
        {
            //the generated regular expressions are replaced by ones built at run time on this
            //target, so this is the only place that branch is executed at all. The check runs when
            //the transport builds its connection information rather than when the QueueConnection
            //is constructed, and the container wraps whatever it throws, so the reason is asserted
            //on the message: a name refused for some unrelated reason would not prove the pattern
            //ran.
            try
            {
                using (var container = new QueueContainer<MemoryBasic.MemoryMessageQueueInit>())
                using (container.CreateProducer<SmokeMessage>(new QueueConnection("not a valid name!", "memory")))
                {
                }

                Assert.Fail("a queue name containing spaces and an exclamation mark was accepted");
            }
            catch (Exception error) when (!(error is AssertFailedException))
            {
                Assert.IsTrue(
                    error.ToString().IndexOf("invalid characters", StringComparison.OrdinalIgnoreCase) >= 0,
                    "the name was refused, but not by the validator: " + error);
            }
        }

        [TestMethod]
        public void The_Aes_Interceptor_Round_Trips()
        {
            //AES-GCM is supplied by BouncyCastle on this framework. The net10.0 suite proves the
            //two implementations agree; this proves the one that ships here actually runs.
            var key = new byte[32];
            for (var i = 0; i < key.Length; i++)
                key[i] = (byte)i;

            var interceptor = new AesMessageInterceptor(new AesMessageInterceptorConfiguration(key));
            var message = System.Text.Encoding.UTF8.GetBytes("a message that has to survive the framework it is read on");

            var encrypted = interceptor.MessageToBytes(message, null);
            Assert.IsTrue(encrypted.AddToGraph,
                "the interceptor did not record itself, so a consumer would never decrypt the message");
            CollectionAssert.AreNotEqual(message, encrypted.Output, "the output is not encrypted");

            CollectionAssert.AreEqual(message, interceptor.BytesToMessage(encrypted.Output, null));
        }

        /// <summary>Sends one message and waits for a consumer to hand it back.</summary>
        private static void RoundTrip<TInit>(QueueConnection connection, string what)
            where TInit : ITransportInit, new()
        {
            var body = Guid.NewGuid().ToString("N");
            var received = new ManualResetEventSlim(false);
            string got = null;

            using (var container = new QueueContainer<TInit>())
            {
                using (var producer = container.CreateProducer<SmokeMessage>(connection))
                {
                    var sent = producer.Send(new SmokeMessage { Body = body });
                    Assert.IsFalse(sent.HasError, sent.SendingException?.ToString() ?? "send reported an error");
                }

                using (var consumer = container.CreateConsumer(connection))
                {
                    consumer.Start<SmokeMessage>((message, notification) =>
                    {
                        got = message.Body.Body;
                        received.Set();
                    }, null);

                    Assert.IsTrue(received.Wait(ReceiveWindow),
                        $"{what}: nothing was received within {ReceiveWindow.TotalSeconds:F0} seconds");
                }
            }

            Assert.AreEqual(body, got, what);
        }

        private static string NewQueueName() => "smoke" + Guid.NewGuid().ToString("N");

        private static string NewDatabaseFile(string extension) =>
            Path.Combine(Path.GetTempPath(), "dnwq-smoke-" + Guid.NewGuid().ToString("N") + extension);

        private static void Delete(string file)
        {
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch (IOException)
            {
                //a held file handle is not what this suite is reporting on
            }
        }

        /// <summary>The payload. Serialised by the library, so it has to be public.</summary>
        public class SmokeMessage
        {
            public string Body { get; set; }
        }
    }
}
