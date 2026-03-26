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
    /// Exercises the rawExpression=true async code paths in ProducerMethodQueue.SendAsync
    /// which use MessageExpressionPayloads.ActionRaw instead of serializing the expression.
    /// </summary>
    [TestClass]
    public class SimpleMethodProducerAsyncRawExpression
    {
        [TestMethod]
        public async Task Run_SendAsync_Single_RawExpression()
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
                                // SendAsync single raw expression
                                var sendResult = await queue.SendAsync(
                                    (message, workerNotification) => Console.WriteLine("async raw single"),
                                    null, true).ConfigureAwait(false);
                                Assert.IsFalse(sendResult.HasError);
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
        public async Task Run_SendAsync_Batch_RawExpression_WithData()
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
                                // SendAsync batch of raw expressions with additional message data
                                var messages =
                                    new List<QueueMessage<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>,
                                        IAdditionalMessageData>>
                                    {
                                        new QueueMessage<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>,
                                            IAdditionalMessageData>(
                                            (message, workerNotification) => Console.WriteLine("async raw batch 1"), null),
                                        new QueueMessage<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>,
                                            IAdditionalMessageData>(
                                            (message, workerNotification) => Console.WriteLine("async raw batch 2"), null)
                                    };

                                var sendResult = await queue.SendAsync(messages, true).ConfigureAwait(false);
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
        public async Task Run_SendAsync_Batch_RawExpression_ListOnly()
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
                                // SendAsync batch of raw expressions without additional data
                                var methods =
                                    new List<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>
                                    {
                                        (message, workerNotification) => Console.WriteLine("async raw list 1"),
                                        (message, workerNotification) => Console.WriteLine("async raw list 2"),
                                        (message, workerNotification) => Console.WriteLine("async raw list 3")
                                    };

                                var sendResult = await queue.SendAsync(methods, true).ConfigureAwait(false);
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
