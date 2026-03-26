using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if NETFULL
namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.ProducerMethod
{
    /// <summary>
    /// Exercises the dynamic LINQ expression Send/SendAsync list overloads in ProducerMethodQueue.
    /// These test the LinqExpressionToRun-based batch code paths (Send(List&lt;LinqExpressionToRun&gt;),
    /// Send(List&lt;QueueMessage&lt;LinqExpressionToRun&gt;&gt;), and their async counterparts).
    /// </summary>
    [TestClass]
    public class SimpleMethodProducerDynamicListSend
    {
        [TestMethod]
        public void Run_Send_DynamicExpressionList()
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
                                var id = Guid.NewGuid();
                                // Send list of dynamic LINQ expressions
                                var methods = new List<LinqExpressionToRun>
                                {
                                    GenerateMethod.CreateDynamic(id, 0),
                                    GenerateMethod.CreateDynamic(id, 0),
                                    GenerateMethod.CreateDynamic(id, 0)
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
        public void Run_Send_DynamicExpressionListWithData()
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
                                var id = Guid.NewGuid();
                                // Send list of dynamic LINQ expressions with per-message data
                                var messages = new List<QueueMessage<LinqExpressionToRun, IAdditionalMessageData>>
                                {
                                    new QueueMessage<LinqExpressionToRun, IAdditionalMessageData>(
                                        GenerateMethod.CreateDynamic(id, 0), null),
                                    new QueueMessage<LinqExpressionToRun, IAdditionalMessageData>(
                                        GenerateMethod.CreateDynamic(id, 0), null)
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
        public async Task Run_SendAsync_DynamicExpressionSingle()
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
                                var id = Guid.NewGuid();
                                // SendAsync single dynamic LINQ expression
                                var sendResult = await queue.SendAsync(GenerateMethod.CreateDynamic(id, 0))
                                    .ConfigureAwait(false);
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
        public async Task Run_SendAsync_DynamicExpressionList()
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
                                var id = Guid.NewGuid();
                                // SendAsync list of dynamic LINQ expressions
                                var methods = new List<LinqExpressionToRun>
                                {
                                    GenerateMethod.CreateDynamic(id, 0),
                                    GenerateMethod.CreateDynamic(id, 0),
                                    GenerateMethod.CreateDynamic(id, 0)
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
        public async Task Run_SendAsync_DynamicExpressionListWithData()
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
                                var id = Guid.NewGuid();
                                // SendAsync list of dynamic LINQ expressions with per-message data
                                var messages = new List<QueueMessage<LinqExpressionToRun, IAdditionalMessageData>>
                                {
                                    new QueueMessage<LinqExpressionToRun, IAdditionalMessageData>(
                                        GenerateMethod.CreateDynamic(id, 0), null),
                                    new QueueMessage<LinqExpressionToRun, IAdditionalMessageData>(
                                        GenerateMethod.CreateDynamic(id, 0), null)
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
#endif
