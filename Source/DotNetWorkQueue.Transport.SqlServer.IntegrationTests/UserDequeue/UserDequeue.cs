using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using Microsoft.Data.SqlClient;
using System.Data;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Integration.Tests.UserDequeue
{
    [Collection("ConsumerUserDequeue")]
    public class UserDequeue
    {
        [Theory]
        [InlineData(250, 0, 180, 1, false, 4, false),
         InlineData(25, 3, 180, 2, false, 4, false)]
        public void Run(int messageCount, int runtime, int timeOut, int readerCount,
            bool useTransactions, int count, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.UserDequeue.Implementation.UserDequeueTests();
            consumer.Run<SqlServerMessageQueueInit, SqlServerMessageQueueCreation>(new QueueConnection(queueName, ConnectionInfo.ConnectionString),
                messageCount, runtime, timeOut, readerCount, count, enableChaos, x => Helpers.SetOptions(x,
                    true, !useTransactions, useTransactions,
                    false,
                    false, !useTransactions, true, true, false, true),
                Helpers.GenerateDataWithColumnValue, Helpers.Verify, Helpers.VerifyQueueCount, SetQueueOptions);

        }

        private void SetQueueOptions(QueueConsumerConfiguration obj, int orderId)
        {
            var SqlParam = new SqlParameter("@OrderID", SqlDbType.Int)
            {
                Value = orderId
            };
            obj.AddUserParameter(SqlParam);
            obj.SetUserWhereClause("(OrderID = @OrderID)");
        }
    }
}
