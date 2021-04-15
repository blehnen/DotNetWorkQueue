using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.ConsumerMethod
{
    [Collection("Consumer")]
    public class ConsumerMethodErrorTable
    {
        [Theory]
        [InlineData(10, 60, 5, LinqMethodTypes.Dynamic, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(1, 60, 1, LinqMethodTypes.Compiled, true, IntegrationConnectionInfo.ConnectionTypes.Memory)]
        public void Run(int messageCount, int timeOut, int workerCount, LinqMethodTypes linqMethodTypes,
            bool enableChaos, IntegrationConnectionInfo.ConnectionTypes connectionType)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(connectionType))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod.Implementation.
                        ConsumerMethodErrorTable();
                consumer.Run<LiteDbMessageQueueInit, LiteDbMessageQueueCreation>(queueName,
                    connectionInfo.ConnectionString,
                    messageCount, timeOut, workerCount, linqMethodTypes, enableChaos, x => Helpers.SetOptions(x, true, false, true),
                    Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount, ValidateErrorCounts);
            }
        }

        private void ValidateErrorCounts(string queueName, string connectionString, int messageCount, ICreationScope scope)
        {
            new VerifyErrorCounts(queueName, connectionString, scope).Verify(messageCount, 2);
        }
    }
}
