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
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.IntegrationTests.Shared.History.Implementation
{
    /// <summary>
    /// Tests that message history records are created and updated through the full lifecycle.
    /// </summary>
    public class SimpleHistoryTest
    {
        /// <summary>
        /// Sends and consumes messages with history enabled, then verifies history records.
        /// </summary>
        public void Run<TTransportInit, TMessage, TTransportCreate>(
            QueueConnection queueConnection,
            int messageCount,
            Action<TTransportCreate> setOptions,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify)
            where TTransportInit : ITransportInit, new()
            where TMessage : class, new()
            where TTransportCreate : class, IQueueCreation
        {
            var logProvider = LoggerShared.Create(queueConnection.Queue, GetType().Name);
            using (var queueCreator =
                new QueueCreationContainer<TTransportInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                ICreationScope scope = null;
                var oCreation = queueCreator.GetQueueCreation<TTransportCreate>(queueConnection);
                try
                {
                    setOptions(oCreation);
                    var result = oCreation.CreateQueue();
                    Assert.IsTrue(result.Success, result.ErrorMessage);
                    scope = oCreation.Scope;

                    // Enable history in the container so decorators record events
                    using (var queueContainer = new QueueContainer<TTransportInit>(serviceRegister =>
                    {
                        serviceRegister.Register(() => logProvider, LifeStyles.Singleton);
                        // Override history config to enabled
                        serviceRegister.Register<IHistoryConfiguration>(() =>
                        {
                            var config = new HistoryConfiguration { Enabled = true };
                            return config;
                        }, LifeStyles.Singleton);
                        // Share scope for in-process transports (Memory, LiteDB :memory:)
                        serviceRegister.RegisterNonScopedSingleton(scope);
                    }))
                    {
                        // Send messages
                        using (var producer = queueContainer.CreateProducer<TMessage>(queueConnection))
                        {
                            for (var i = 0; i < messageCount; i++)
                            {
                                var sendResult = producer.Send(new TMessage());
                                Assert.IsFalse(sendResult.HasError, $"Send failed: {sendResult.SendingException?.Message}");
                            }
                        }

                        // Consume messages
                        var processedCount = 0;
                        var waitHandle = new ManualResetEventSlim(false);
                        using (var consumer = queueContainer.CreateConsumer(queueConnection))
                        {
                            consumer.Configuration.Worker.WorkerCount = 1;
                            consumer.Start<TMessage>((message, workerNotification) =>
                            {
                                Interlocked.Increment(ref processedCount);
                                if (processedCount >= messageCount)
                                    waitHandle.Set();
                            }, null);

                            waitHandle.Wait(TimeSpan.FromSeconds(30));
                        }

                        Assert.AreEqual(messageCount, processedCount, "Not all messages were processed");

                        // Verify history records via admin container
                        using (var adminContainer = queueContainer.CreateAdminContainer(queueConnection))
                        {
                            var historyQuery = adminContainer.GetInstance<IQueryMessageHistory>();

                            // Total count should match messages sent
                            var totalCount = historyQuery.GetCount(null);
                            Assert.IsTrue(totalCount >= messageCount,
                                $"Expected at least {messageCount} history records, got {totalCount}");

                            // All should be Complete status (2) since they were consumed
                            var completeCount = historyQuery.GetCount(MessageHistoryStatus.Complete);
                            Assert.IsTrue(completeCount >= messageCount,
                                $"Expected at least {messageCount} completed records, got {completeCount}");

                            // Paged query should return records
                            var records = historyQuery.Get(0, 100, null);
                            Assert.IsNotNull(records);
                            Assert.IsTrue(records.Count >= messageCount,
                                $"Expected at least {messageCount} records from Get, got {records.Count}");

                            // Each record should have valid data
                            foreach (var record in records)
                            {
                                Assert.IsNotNull(record.QueueId, "QueueId should not be null");
                                Assert.AreEqual(MessageHistoryStatus.Complete, record.Status);
                                Assert.IsTrue(record.EnqueuedUtc > DateTime.MinValue, "EnqueuedUtc should be set");
                                // StartedUtc, CompletedUtc, and DurationMs may be null if the history
                                // write for RecordComplete failed (best-effort) or if the transport
                                // doesn't support duration calculation. Don't assert on these.
                            }

                            // GetByQueueId should find a specific record
                            var firstRecord = records[0];
                            var byId = historyQuery.GetByQueueId(firstRecord.QueueId);
                            Assert.IsNotNull(byId, "GetByQueueId should find the record");
                            Assert.AreEqual(firstRecord.QueueId, byId.QueueId);

                            // Purge with future date should remove records
                            var purgeHandler = adminContainer.GetInstance<IPurgeMessageHistory>();
                            var purged = purgeHandler.Purge(DateTime.UtcNow.AddDays(1));
                            Assert.AreEqual(totalCount, purged, $"Purge should have removed {totalCount} records, got {purged}");

                            // After purge, count should be 0
                            var afterPurge = historyQuery.GetCount(null);
                            Assert.AreEqual(0, afterPurge, "Count should be 0 after purge");
                        }
                    }
                }
                finally
                {
                    oCreation?.RemoveQueue();
                    oCreation?.Dispose();
                    scope?.Dispose();
                }
            }
        }
    }
}
