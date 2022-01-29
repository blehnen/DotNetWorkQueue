using System.Data;
using System.Data.SqlClient;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.UserDequeue
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
            consumer.Run<PostgreSqlMessageQueueInit, PostgreSqlMessageQueueCreation>(new QueueConnection(queueName, ConnectionInfo.ConnectionString),
                messageCount, runtime, timeOut, readerCount, count, enableChaos, x => Helpers.SetOptions(x,
                    true, !useTransactions, useTransactions, false,
                    false, !useTransactions, true, true, false, true),
                Helpers.GenerateDataWithColumnValue, Helpers.Verify, Helpers.VerifyQueueCount, SetQueueOptions);

        }

        private void SetQueueOptions(QueueConsumerConfiguration obj, int orderId)
        {
            var SqlParam = new Npgsql.NpgsqlParameter("@OrderID", orderId);
            obj.AddUserParameter(SqlParam);
            obj.SetUserWhereClause("(q.OrderID = @OrderID)");
        }
    }
}
