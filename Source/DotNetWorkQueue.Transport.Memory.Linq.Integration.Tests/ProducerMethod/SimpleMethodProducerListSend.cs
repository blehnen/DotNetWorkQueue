using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.ProducerMethod
{
    /// <summary>
    /// Exercises the Send(List&lt;Expression&gt;) and SendAsync(List&lt;Expression&gt;) overloads
    /// in ProducerMethodQueue that send a list of expressions without additional per-message data.
    /// These are separate code paths from the batch-with-data overloads.
    /// </summary>
    [TestClass]
    public class SimpleMethodProducerListSend
    {
        [TestMethod]
        public void Run_Send_ExpressionList()
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                using (var queueCreator =
                    new QueueCreationContainer<MemoryMessageQueueInit>())
                {
                    var oCreation = queueCreator.GetQueueCreation<MessageQueueCreation>(new QueueConnection(queueName,
                        connectionInfo.ConnectionString));
                    var scope = oCreation.Scope;
                    try
                    {
                        var result = oCreation.CreateQueue();
                        Assert.IsTrue(result.Success, result.ErrorMessage);

                        using (var queueContainer = new QueueContainer<MemoryMessageQueueInit>(
                            x => x.RegisterNonScopedSingleton(scope)))
                        {
                            using (var queue = queueContainer.CreateMethodProducer(new QueueConnection(queueName,
                                connectionInfo.ConnectionString)))
                            {
                                // Send list of compiled expressions (non-raw, no per-message data)
                                var methods =
                                    new List<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>
                                    {
                                        (message, workerNotification) => Console.WriteLine("list 1"),
                                        (message, workerNotification) => Console.WriteLine("list 2"),
                                        (message, workerNotification) => Console.WriteLine("list 3"),
                                        (message, workerNotification) => Console.WriteLine("list 4"),
                                        (message, workerNotification) => Console.WriteLine("list 5")
                                    };

                                var sendResult = queue.Send(methods);
                                Assert.IsFalse(sendResult.HasErrors);
                            }
                        }
                    }
                    finally
                    {
                        oCreation.RemoveQueue();
                        oCreation.Dispose();
                        scope?.Dispose();
                    }
                }
            }
        }

        [TestMethod]
        public async Task Run_SendAsync_ExpressionList()
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                using (var queueCreator =
                    new QueueCreationContainer<MemoryMessageQueueInit>())
                {
                    var oCreation = queueCreator.GetQueueCreation<MessageQueueCreation>(new QueueConnection(queueName,
                        connectionInfo.ConnectionString));
                    var scope = oCreation.Scope;
                    try
                    {
                        var result = oCreation.CreateQueue();
                        Assert.IsTrue(result.Success, result.ErrorMessage);

                        using (var queueContainer = new QueueContainer<MemoryMessageQueueInit>(
                            x => x.RegisterNonScopedSingleton(scope)))
                        {
                            using (var queue = queueContainer.CreateMethodProducer(new QueueConnection(queueName,
                                connectionInfo.ConnectionString)))
                            {
                                // SendAsync list of compiled expressions (non-raw, no per-message data)
                                var methods =
                                    new List<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>
                                    {
                                        (message, workerNotification) => Console.WriteLine("async list 1"),
                                        (message, workerNotification) => Console.WriteLine("async list 2"),
                                        (message, workerNotification) => Console.WriteLine("async list 3")
                                    };

                                var sendResult = await queue.SendAsync(methods).ConfigureAwait(false);
                                Assert.IsFalse(sendResult.HasErrors);
                            }
                        }
                    }
                    finally
                    {
                        oCreation.RemoveQueue();
                        oCreation.Dispose();
                        scope?.Dispose();
                    }
                }
            }
        }

        [TestMethod]
        public void Run_Send_BatchWithData()
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                using (var queueCreator =
                    new QueueCreationContainer<MemoryMessageQueueInit>())
                {
                    var oCreation = queueCreator.GetQueueCreation<MessageQueueCreation>(new QueueConnection(queueName,
                        connectionInfo.ConnectionString));
                    var scope = oCreation.Scope;
                    try
                    {
                        var result = oCreation.CreateQueue();
                        Assert.IsTrue(result.Success, result.ErrorMessage);

                        using (var queueContainer = new QueueContainer<MemoryMessageQueueInit>(
                            x => x.RegisterNonScopedSingleton(scope)))
                        {
                            using (var queue = queueContainer.CreateMethodProducer(new QueueConnection(queueName,
                                connectionInfo.ConnectionString)))
                            {
                                // Send batch of expressions with per-message data (non-raw)
                                var messages =
                                    new List<QueueMessage<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>,
                                        IAdditionalMessageData>>
                                    {
                                        new QueueMessage<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>,
                                            IAdditionalMessageData>(
                                            (message, workerNotification) => Console.WriteLine("batch data 1"), null),
                                        new QueueMessage<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>,
                                            IAdditionalMessageData>(
                                            (message, workerNotification) => Console.WriteLine("batch data 2"), null)
                                    };

                                var sendResult = queue.Send(messages);
                                Assert.IsFalse(sendResult.HasErrors);
                            }
                        }
                    }
                    finally
                    {
                        oCreation.RemoveQueue();
                        oCreation.Dispose();
                        scope?.Dispose();
                    }
                }
            }
        }

        [TestMethod]
        public async Task Run_SendAsync_BatchWithData()
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                using (var queueCreator =
                    new QueueCreationContainer<MemoryMessageQueueInit>())
                {
                    var oCreation = queueCreator.GetQueueCreation<MessageQueueCreation>(new QueueConnection(queueName,
                        connectionInfo.ConnectionString));
                    var scope = oCreation.Scope;
                    try
                    {
                        var result = oCreation.CreateQueue();
                        Assert.IsTrue(result.Success, result.ErrorMessage);

                        using (var queueContainer = new QueueContainer<MemoryMessageQueueInit>(
                            x => x.RegisterNonScopedSingleton(scope)))
                        {
                            using (var queue = queueContainer.CreateMethodProducer(new QueueConnection(queueName,
                                connectionInfo.ConnectionString)))
                            {
                                // SendAsync batch of expressions with per-message data (non-raw)
                                var messages =
                                    new List<QueueMessage<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>,
                                        IAdditionalMessageData>>
                                    {
                                        new QueueMessage<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>,
                                            IAdditionalMessageData>(
                                            (message, workerNotification) => Console.WriteLine("async batch data 1"), null),
                                        new QueueMessage<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>,
                                            IAdditionalMessageData>(
                                            (message, workerNotification) => Console.WriteLine("async batch data 2"), null)
                                    };

                                var sendResult = await queue.SendAsync(messages).ConfigureAwait(false);
                                Assert.IsFalse(sendResult.HasErrors);
                            }
                        }
                    }
                    finally
                    {
                        oCreation.RemoveQueue();
                        oCreation.Dispose();
                        scope?.Dispose();
                    }
                }
            }
        }
    }
}
