using System;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests.Consumer
{
    /// <summary>
    /// Tests consumer behavior when messages throw exceptions.
    /// The Memory transport does not support retry/rollback, so messages that throw
    /// are moved directly to the error state. This test verifies that the consumer
    /// continues processing subsequent messages after an error occurs.
    /// </summary>
    [TestClass]
    public class ConsumerRollBack
    {
        [TestMethod]
        public void Consumer_Continues_Processing_After_Error()
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var queueConnection = new QueueConnection(queueName, connectionInfo.ConnectionString);

                using (var creator = new QueueCreationContainer<MemoryMessageQueueInit>())
                {
                    using (var creation = creator.GetQueueCreation<MessageQueueCreation>(queueConnection))
                    {
                        creation.CreateQueue();
                        var scope = creation.Scope;

                        // Send 5 messages
                        const int messageCount = 5;
                        using (var queueContainer = new QueueContainer<MemoryMessageQueueInit>(
                            serviceRegister => serviceRegister.RegisterNonScopedSingleton(scope)))
                        {
                            using (var producer = queueContainer.CreateProducer<FakeMessage>(queueConnection))
                            {
                                for (var i = 0; i < messageCount; i++)
                                {
                                    var result = producer.Send(new FakeMessage());
                                    Assert.IsFalse(result.HasError);
                                }
                            }
                        }

                        // Consume: first 2 messages throw, rest succeed
                        var processedCount = 0;
                        var errorCount = 0;
                        var allDone = new ManualResetEventSlim(false);

                        using (var queueContainer = new QueueContainer<MemoryMessageQueueInit>(
                            serviceRegister => serviceRegister.RegisterNonScopedSingleton(scope)))
                        {
                            using (var consumer = queueContainer.CreateConsumer(queueConnection))
                            {
                                consumer.Configuration.Worker.WorkerCount = 1;
                                consumer.Configuration.Worker.SingleWorkerWhenNoWorkFound = true;

                                var messageIndex = 0;
                                consumer.Start<FakeMessage>((message, notifications) =>
                                {
                                    var idx = Interlocked.Increment(ref messageIndex);
                                    if (idx <= 2)
                                    {
                                        Interlocked.Increment(ref errorCount);
                                        throw new InvalidOperationException($"Intentional error for message {idx}");
                                    }

                                    if (Interlocked.Increment(ref processedCount) >= messageCount - 2)
                                        allDone.Set();
                                }, new ConsumerQueueNotifications());

                                Assert.IsTrue(allDone.Wait(TimeSpan.FromSeconds(30)),
                                    "consumer should process remaining messages after errors");
                            }
                        }

                        Assert.AreEqual(3, processedCount, "3 messages should be processed successfully");
                        Assert.AreEqual(2, errorCount, "2 messages should have thrown errors");
                    }
                }
            }
        }

        [TestMethod]
        public void Consumer_Handles_Cancellation_Token_Stop()
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var queueConnection = new QueueConnection(queueName, connectionInfo.ConnectionString);

                using (var creator = new QueueCreationContainer<MemoryMessageQueueInit>())
                {
                    using (var creation = creator.GetQueueCreation<MessageQueueCreation>(queueConnection))
                    {
                        creation.CreateQueue();
                        var scope = creation.Scope;

                        // Send 3 messages
                        using (var queueContainer = new QueueContainer<MemoryMessageQueueInit>(
                            serviceRegister => serviceRegister.RegisterNonScopedSingleton(scope)))
                        {
                            using (var producer = queueContainer.CreateProducer<FakeMessage>(queueConnection))
                            {
                                for (var i = 0; i < 3; i++)
                                {
                                    producer.Send(new FakeMessage());
                                }
                            }
                        }

                        // Consume, but stop the consumer after the first message is processed
                        var processedCount = 0;
                        var firstMessageProcessed = new ManualResetEventSlim(false);

                        using (var queueContainer = new QueueContainer<MemoryMessageQueueInit>(
                            serviceRegister => serviceRegister.RegisterNonScopedSingleton(scope)))
                        {
                            using (var consumer = queueContainer.CreateConsumer(queueConnection))
                            {
                                consumer.Configuration.Worker.WorkerCount = 1;
                                consumer.Configuration.Worker.SingleWorkerWhenNoWorkFound = true;

                                consumer.Start<FakeMessage>((message, notifications) =>
                                {
                                    Interlocked.Increment(ref processedCount);
                                    firstMessageProcessed.Set();
                                    // Block so consumer only picks up one at a time
                                    Thread.Sleep(200);
                                }, new ConsumerQueueNotifications());

                                // Wait for first message then stop
                                Assert.IsTrue(firstMessageProcessed.Wait(TimeSpan.FromSeconds(10)),
                                    "at least one message should be processed");
                            }
                            // Consumer disposed -- no crash
                        }

                        Assert.IsTrue(processedCount >= 1,
                            "at least one message should have been processed before stop");
                    }
                }
            }
        }
    }
}
