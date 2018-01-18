using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests
{
    public static class Helpers
    {
        public static void Verify(string notused1, string notused2, QueueProducerConfiguration config, long recordCount, ICreationScope scope)
        {
            var realScope = (CreationScope) scope;
            if (realScope.ContainedClears.TryPeek(out var dataStorage))
            {
                var data = (IDataStorage)dataStorage;
                var messageCount = data.RecordCount;
                Assert.Equal(recordCount, messageCount);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static void NoVerification(string queueName, string connectionString, QueueProducerConfiguration queueProducerConfiguration, long messageCount, ICreationScope scope)
        {
            
        }

        public static AdditionalMessageData GenerateData(QueueProducerConfiguration configuration)
        {
            return null;
        }
    }

    public class IncrementWrapper
    {
        public IncrementWrapper()
        {
            ProcessedCount = 0;
        }
        public long ProcessedCount;
    }
}
