using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SQLite;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.UserDequeue
{
    /// <summary>
    /// The same dequeue filter as <see cref="UserDequeue"/>, supplied through the factory form of
    /// the API rather than as static values. The factories are meant to be consulted on every
    /// dequeue, which the script cache in ReceiveMessageQueryHandler has to preserve.
    /// </summary>
    [TestClass]
    public class UserDequeueFromFactory
    {
        private readonly ConcurrentDictionary<int, int> _clauseCalls = new ConcurrentDictionary<int, int>();
        private readonly ConcurrentDictionary<int, int> _parameterCalls = new ConcurrentDictionary<int, int>();

        [TestMethod]
        [DataRow(100, 0, 240, 1, false, 4, false),
         DataRow(25, 3, 240, 2, true, 4, false)]
        public void Run(int messageCount, int runtime, int timeOut, int readerCount,
            bool inMemoryDb, int valueCount, bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var producer =
                    new DotNetWorkQueue.IntegrationTests.Shared.UserDequeue.Implementation.UserDequeueTests();
                producer.Run<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(
                    new QueueConnection(queueName, connectionInfo.ConnectionString), messageCount, runtime, timeOut,
                    readerCount, valueCount, enableChaos, x => Helpers.SetOptions(x,
                        false, true, false, false, true, true, true, false, true),
                    Helpers.GenerateDataWithColumnValue, Helpers.Verify, Helpers.VerifyQueueCount, SetQueueOptions);
            }

            //The messages were all consumed, so the filter worked. These assert the stronger thing:
            //that the factories were consulted repeatedly rather than once and cached. Without them
            //this test would pass even if the clause and the parameter list were both cached, since
            //each consumer has its own handler and its own values.
            Assert.HasCount(valueCount, _clauseCalls, "every consumer should have used its clause factory");
            Assert.HasCount(valueCount, _parameterCalls, "every consumer should have used its parameter factory");

            foreach (var calls in _clauseCalls)
                Assert.IsGreaterThan(1, calls.Value,
                    $"the clause factory for order {calls.Key} was called {calls.Value} time(s); it must be consulted per dequeue");

            foreach (var calls in _parameterCalls)
                Assert.IsGreaterThan(1, calls.Value,
                    $"the parameter factory for order {calls.Key} was called {calls.Value} time(s); it must be consulted per dequeue");
        }

        private void SetQueueOptions(QueueConsumerConfiguration obj, int orderId)
        {
            obj.SetUserParametersAndClause(
                () =>
                {
                    _parameterCalls.AddOrUpdate(orderId, 1, (_, count) => count + 1);
                    return new List<SQLiteParameter> { new SQLiteParameter("@OrderID", orderId) };
                },
                () =>
                {
                    _clauseCalls.AddOrUpdate(orderId, 1, (_, count) => count + 1);
                    return "(OrderID = @OrderID)";
                });
        }
    }
}
