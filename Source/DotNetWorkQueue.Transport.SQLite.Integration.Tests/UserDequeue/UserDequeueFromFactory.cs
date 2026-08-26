using System.Collections.Generic;
using System.Data.SQLite;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.UserDequeue
{
    /// <summary>
    /// The same dequeue filter as <see cref="UserDequeue"/>, but supplied through the factory form
    /// of the API rather than as static values. The factories are meant to be consulted on every
    /// dequeue, which the script cache in ReceiveMessageQueryHandler has to preserve.
    /// </summary>
    [TestClass]
    public class UserDequeueFromFactory
    {
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
        }

        private void SetQueueOptions(QueueConsumerConfiguration obj, int orderId)
        {
            //deliberately built fresh on every call, so a cached clause or a cached parameter list
            //would be visible as wrong results rather than as a silent behaviour change
            obj.SetUserParametersAndClause(
                () => new List<SQLiteParameter> { new SQLiteParameter("@OrderID", orderId) },
                () => "(OrderID = @OrderID)");
        }
    }
}
