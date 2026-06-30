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
using System.Diagnostics;
using System.Linq;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Producer
{
    /// <summary>
    /// Lightweight, premise-proving benchmark: a loop of single <c>Send</c> calls versus one true
    /// batch <c>Send(List)</c> for the same message count. Not a rigorous micro-benchmark — it
    /// captures the headline throughput win and logs it. The pass/fail check is correctness (both
    /// paths write every row); the wall-clock comparison is logged as informational only, since a
    /// hard timing gate is flaky on shared CI runners. Requires a running PostgreSQL with
    /// <c>connectionstring.txt</c> configured.
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
            // Informational only — each scenario already asserts all rows were written (correctness).
            // No hard timing assertion: wall-clock comparisons are flaky on shared CI runners.
            TestContext.WriteLine(line);
            Console.WriteLine(line);
        }

        private static long RunScenario(bool sendViaBatch, int count)
        {
            var queueName = GenerateQueueName.Create();
            var queueConnection = new QueueConnection(queueName, ConnectionInfo.ConnectionString);
            using (var queueCreator = new QueueCreationContainer<PostgreSqlMessageQueueInit>())
            using (var oCreation = queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection))
            {
                Assert.IsTrue(oCreation.CreateQueue().Success);
                try
                {
                    using (var qc = new QueueContainer<PostgreSqlMessageQueueInit>())
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
                        // Correctness gate: both paths must persist every message.
                        new VerifyQueueData(queueName, oCreation.Options).Verify(count, null);
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
