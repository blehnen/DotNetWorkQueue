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
using DotNetWorkQueue.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.IntegrationTests.Shared.History.Implementation
{
    public class SimpleHistoryTest
    {
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

                    // Send and consume in one container
                    var processedCount = 0;
                    using (var queueContainer = new QueueContainer<TTransportInit>(serviceRegister =>
                    {
                        serviceRegister.Register(() => logProvider, LifeStyles.Singleton);
                        serviceRegister.RegisterNonScopedSingleton(scope);
                    }))
                    {
                        using (var producer = queueContainer.CreateProducer<TMessage>(queueConnection))
                        {
                            for (var i = 0; i < messageCount; i++)
                            {
                                var sendResult = producer.Send(new TMessage());
                                Assert.IsFalse(sendResult.HasError, $"Send failed: {sendResult.SendingException?.Message}");
                            }
                        }

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
                    }
                    // QueueContainer disposed — all LiteDB/DB connections flushed

                    Assert.AreEqual(messageCount, processedCount, "Not all messages were processed");

                    // Dispose creation objects first to release DB connections
                    oCreation?.Dispose();
                    oCreation = null;

                    // Verify history in a FRESH container
                    using (var verifyContainer = new QueueContainer<TTransportInit>(serviceRegister =>
                    {
                        serviceRegister.Register(() => logProvider, LifeStyles.Singleton);
                        serviceRegister.RegisterNonScopedSingleton(scope);
                    }))
                    {
                        using (var adminContainer = verifyContainer.CreateAdminContainer(queueConnection))
                        {
                            var adminHistoryConfig = adminContainer.GetInstance<IHistoryConfiguration>();
                            adminHistoryConfig.Enabled = true;

                            var historyQuery = adminContainer.GetInstance<IQueryMessageHistory>();

                            var totalCount = historyQuery.GetCount(null);
                            Assert.IsTrue(totalCount >= messageCount,
                                $"Expected at least {messageCount} history records, got {totalCount}");

                            var completeCount = historyQuery.GetCount(MessageHistoryStatus.Complete);
                            Assert.IsTrue(completeCount >= messageCount,
                                $"Expected at least {messageCount} completed records, got {completeCount}");

                            var records = historyQuery.Get(0, 100, null);
                            Assert.IsNotNull(records);
                            Assert.IsTrue(records.Count >= messageCount);

                            foreach (var record in records)
                            {
                                Assert.IsNotNull(record.QueueId);
                                Assert.AreEqual(MessageHistoryStatus.Complete, record.Status);
                            }

                            var firstRecord = records[0];
                            var byId = historyQuery.GetByQueueId(firstRecord.QueueId);
                            Assert.IsNotNull(byId);

                            var purgeHandler = adminContainer.GetInstance<IPurgeMessageHistory>();
                            var purged = purgeHandler.Purge(DateTime.UtcNow.AddDays(1));
                            Assert.AreEqual(totalCount, purged);

                            Assert.AreEqual(0, historyQuery.GetCount(null));
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
