using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.UserDequeue
{
    [Collection("ConsumerUserDequeue")]
    public class UserDequeue
    {
        [Theory]
        [InlineData(250, 0, 180, 1, false, 4, false),
         InlineData(25, 3, 180, 2, true, 4, false)]
        public void Run(int messageCount, int runtime, int timeOut, int readerCount,
            bool inMemoryDb, int valueCount, bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var producer =
                    new DotNetWorkQueue.IntegrationTests.Shared.UserDequeue.Implementation.UserDequeueTests();
                producer.Run<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(new QueueConnection(queueName, connectionInfo.ConnectionString), messageCount, runtime, timeOut, readerCount, valueCount,
                    enableChaos, x => Helpers.SetOptions(x,
                        false, true, false, false, true, true, true, false, true),
                    Helpers.GenerateDataWithColumnValue, Helpers.Verify, Helpers.VerifyQueueCount, SetQueueOptions);
            }
        }

        private void SetQueueOptions(QueueConsumerConfiguration obj, int orderId)
        {
            var SqlParam = new SQLiteParameter("@OrderID", orderId);
            obj.AddUserParameter(SqlParam);
            obj.SetUserWhereClause("(OrderID = @OrderID)");
        }
    }
}
