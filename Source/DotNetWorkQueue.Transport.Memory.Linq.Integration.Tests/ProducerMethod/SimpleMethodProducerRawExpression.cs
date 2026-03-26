using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.ProducerMethod
{
    /// <summary>
    /// Exercises the rawExpression=true code paths in ProducerMethodQueue.Send
    /// which use MessageExpressionPayloads.ActionRaw instead of serializing the expression.
    /// This is valid for in-process queues like Memory transport.
    /// </summary>
    [TestClass]
    public class SimpleMethodProducerRawExpression
    {
        [TestMethod]
        public void Run_Send_Single_RawExpression()
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
                                // Send single raw expression
                                var sendResult = queue.Send(
                                    (message, workerNotification) => Console.WriteLine("raw single"),
                                    null, true);
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
        public void Run_Send_Batch_RawExpression_WithData()
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
                                // Send batch of raw expressions with additional message data
                                var messages =
                                    new List<QueueMessage<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>,
                                        IAdditionalMessageData>>
                                    {
                                        new QueueMessage<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>,
                                            IAdditionalMessageData>(
                                            (message, workerNotification) => Console.WriteLine("raw batch 1"), null),
                                        new QueueMessage<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>,
                                            IAdditionalMessageData>(
                                            (message, workerNotification) => Console.WriteLine("raw batch 2"), null),
                                        new QueueMessage<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>,
                                            IAdditionalMessageData>(
                                            (message, workerNotification) => Console.WriteLine("raw batch 3"), null)
                                    };

                                var sendResult = queue.Send(messages, true);
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
        public void Run_Send_Batch_RawExpression_ListOnly()
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
                                // Send batch of raw expressions without additional data
                                var methods =
                                    new List<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>
                                    {
                                        (message, workerNotification) => Console.WriteLine("raw list 1"),
                                        (message, workerNotification) => Console.WriteLine("raw list 2"),
                                        (message, workerNotification) => Console.WriteLine("raw list 3")
                                    };

                                var sendResult = queue.Send(methods, true);
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
