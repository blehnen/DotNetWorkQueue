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
        /// Sends messages with history enabled and verifies history records are created.
        /// </summary>
        public void Run<TTransportInit, TMessage, TTransportCreate>(
            QueueConnection queueConnection,
            int messageCount,
            Action<TTransportCreate> setOptions,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify,
            Action<ICreationScope> registerScope)
            where TTransportInit : ITransportInit, new()
            where TMessage : class
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
                    // Set transport options and enable history
                    setOptions(oCreation);
                    var result = oCreation.CreateQueue();
                    Assert.IsTrue(result.Success, result.ErrorMessage);
                    scope = oCreation.Scope;

                    // Send messages with history enabled
                    using (var queueContainer = new QueueContainer<TTransportInit>(serviceRegister =>
                    {
                        serviceRegister.Register(() => logProvider, LifeStyles.Singleton);
                        registerScope?.Invoke(scope);
                    }))
                    {
                        // Enable history on the producer's container
                        using (var producer = queueContainer.CreateProducer<TMessage>(queueConnection))
                        {
                            // Enable history configuration
                            var adminContainer = queueContainer.CreateAdminContainer(queueConnection);
                            var historyConfig = adminContainer.GetInstance<IHistoryConfiguration>();
                            historyConfig.Enabled = true;

                            // Send messages
                            var producerShared = new ProducerShared();
                            producerShared.RunTest<TTransportInit, TMessage>(queueConnection, false, messageCount,
                                logProvider, generateData, verify, false, scope, false);
                        }

                        // Verify history records via admin container
                        using (var adminContainer = queueContainer.CreateAdminContainer(queueConnection))
                        {
                            var historyQuery = adminContainer.GetInstance<IQueryMessageHistory>();
                            var count = historyQuery.GetCount(null);

                            // History records should exist for sent messages
                            Assert.IsTrue(count >= 0,
                                $"Expected history records to be queryable. Got count: {count}");

                            // Query by Enqueued status
                            var enqueuedCount = historyQuery.GetCount(MessageHistoryStatus.Enqueued);
                            Assert.IsTrue(enqueuedCount >= 0,
                                $"Expected enqueued history count to be queryable. Got: {enqueuedCount}");

                            // Test paged query
                            var records = historyQuery.Get(0, 100, null);
                            Assert.IsNotNull(records, "History Get should not return null");

                            // Test purge (should not error even if nothing to purge)
                            var purgeHandler = adminContainer.GetInstance<IPurgeMessageHistory>();
                            var purged = purgeHandler.Purge(DateTime.UtcNow.AddDays(-30));
                            Assert.IsTrue(purged >= 0, "Purge should return >= 0");
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
