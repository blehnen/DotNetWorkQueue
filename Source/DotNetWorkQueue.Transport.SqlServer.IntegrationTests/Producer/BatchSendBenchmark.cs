using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Producer
{
    /// <summary>
    /// Lightweight, premise-proving benchmark: a loop of single <c>Send</c> calls versus one true
    /// batch <c>Send(List)</c> for the same message count. Not a rigorous micro-benchmark — it
    /// captures the headline throughput win and asserts the batch path is faster. Requires a
    /// running SQL Server with <c>connectionstring.txt</c> configured.
    /// </summary>
    [TestClass]
    public class BatchSendBenchmark
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void LoopOfSend_VersusTrueBatch()
        {
            const int count = 500;

            var loopElapsed = RunScenario(sendViaBatch: false, count);
            var batchElapsed = RunScenario(sendViaBatch: true, count);

            var speedup = (double)loopElapsed / Math.Max(1, batchElapsed);
            var line = $"Batch benchmark ({count} messages): loop={loopElapsed} ms, batch={batchElapsed} ms, speedup={speedup:F1}x";
            TestContext.WriteLine(line);
            Console.WriteLine(line);

            // Premise: the single-transaction batch is faster than N independent transactions.
            Assert.IsTrue(batchElapsed < loopElapsed,
                $"expected the batch path to be faster: {line}");
        }

        private static long RunScenario(bool sendViaBatch, int count)
        {
            var queueName = GenerateQueueName.Create();
            var queueConnection = new QueueConnection(queueName, ConnectionInfo.ConnectionString);
            using (var queueCreator = new QueueCreationContainer<SqlServerMessageQueueInit>())
            using (var oCreation = queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection))
            {
                Assert.IsTrue(oCreation.CreateQueue().Success);
                try
                {
                    using (var qc = new QueueContainer<SqlServerMessageQueueInit>())
                    using (var producer = qc.CreateProducer<FakeMessage>(queueConnection))
                    {
                        var sw = Stopwatch.StartNew();
                        if (sendViaBatch)
                        {
                            var messages = Enumerable.Range(0, count)
                                .Select(i => new QueueMessage<FakeMessage, IAdditionalMessageData>(
                                    new FakeMessage { Name = "msg-" + i }, null))
                                .ToList();
                            var output = producer.Send(messages);
                            Assert.IsFalse(output.HasErrors);
                        }
                        else
                        {
                            for (var i = 0; i < count; i++)
                            {
                                var output = producer.Send(new FakeMessage { Name = "msg-" + i });
                                Assert.IsNull(output.SendingException);
                            }
                        }
                        sw.Stop();
                        return sw.ElapsedMilliseconds;
                    }
                }
                finally
                {
                    oCreation.RemoveQueue();
                }
            }
        }
    }
}
