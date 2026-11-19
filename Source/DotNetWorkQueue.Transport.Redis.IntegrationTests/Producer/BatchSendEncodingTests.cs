using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Interceptors;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Redis.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.Producer
{
    /// <summary>
    /// A batch send keeps binary bodies and non-ASCII headers intact (GitHub #416).
    /// </summary>
    /// <remarks>
    /// The batch path packs its messages with its own MessagePack writer for the script's <c>cmsgpack.unpack</c>;
    /// a single send passes each value to Redis directly. These are the inputs where the two encodings could differ.
    /// </remarks>
    [TestClass]
    [Retry(1)]
    public class BatchSendEncodingTests
    {
        private const int MessageCount = 20;
        private static readonly IMessageContextData<string> TextHeader =
            new MessageContextData<string>("DotNetWorkQueue.Tests.BatchEncoding", null);

        [TestMethod]
        public void A_Batch_Keeps_Binary_Bodies_And_Non_Ascii_Headers()
        {
            var queueConnection = new QueueConnection(GenerateQueueName.Create(), ConnectionInfo.ConnectionString);
            using var creation = new QueueCreationContainer<RedisQueueInit>();
            using var queueCreation = creation.GetQueueCreation<RedisQueueCreation>(queueConnection);
            try
            {
                Assert.IsTrue(queueCreation.CreateQueue().Success);
                var sent = Enumerable.Range(0, MessageCount).Select(i =>
                {
                    var data = new AdditionalMessageData();
                    data.SetHeader(TextHeader, HeaderText(i));
                    return new QueueMessage<EncodingMessage, IAdditionalMessageData>(Body(i), data);
                }).ToList();

                var received = new ConcurrentDictionary<int, (EncodingMessage Body, string Header)>();
                //gzip makes each stored body binary; the bodies are long enough to pass its minimum size
                using (var container = new QueueContainer<RedisQueueInit>(r =>
                           r.RegisterCollection<IMessageInterceptor>(new[] { typeof(GZipMessageInterceptor) })))
                {
                    using (var producer = container.CreateProducer<EncodingMessage>(queueConnection))
                    {
                        var result = producer.Send(sent);
                        Assert.IsFalse(result.HasErrors,
                            result.FirstOrDefault(r => r.SendingException != null)?.SendingException?.ToString());
                    }

                    using var done = new ManualResetEventSlim();
                    using var consumer = container.CreateConsumer(queueConnection);
                    consumer.Start<EncodingMessage>((message, _) =>
                    {
                        received[message.Body.Index] = (message.Body, message.GetHeader(TextHeader));
                        if (received.Count == MessageCount)
                            done.Set();
                    }, new ConsumerQueueNotifications());
                    Assert.IsTrue(done.Wait(TimeSpan.FromSeconds(60)), $"received {received.Count} of {MessageCount}");
                }

                for (var i = 0; i < MessageCount; i++)
                {
                    Assert.IsTrue(received.TryGetValue(i, out var message), $"message {i} was not received");
                    var expected = Body(i);
                    Assert.AreEqual(expected.Text, message.Body.Text);
                    CollectionAssert.AreEqual(expected.Data, message.Body.Data);
                    Assert.AreEqual(HeaderText(i), message.Header);
                }
            }
            finally
            {
                queueCreation.RemoveQueue();
            }
        }

        private static EncodingMessage Body(int index)
        {
            //every byte value, offset by the index so no two bodies are alike
            var data = Enumerable.Range(0, 256).Select(b => (byte)(b + index)).ToArray();
            return new EncodingMessage
            {
                Index = index,
                Text = string.Concat(Enumerable.Repeat($"Grüße 漢字 😀 {index} ", 20)),
                Data = data
            };
        }

        private static string HeaderText(int index) => $"Überprüfung – 検証 – 🔑 {index}";

        public class EncodingMessage
        {
            public int Index { get; set; }
            public string Text { get; set; }
            public byte[] Data { get; set; }
        }
    }
}
