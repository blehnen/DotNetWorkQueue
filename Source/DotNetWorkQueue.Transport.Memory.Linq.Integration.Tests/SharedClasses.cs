using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests
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

        public static void Verify(string notused1, string notused2, long recordCount, ICreationScope scope)
        {
            var realScope = (CreationScope)scope;
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

        public static void SetError(string queueName, string connectionString, ICreationScope scope)
        {
            //no such thing as an error in the memory queue, as it's a FIFO queue with no rollbacks
            //delete the job instead
            var realScope = (CreationScope)scope;
            if (realScope.ContainedClears.TryPeek(out var dataStorage))
            {
                var data = (IDataStorage) dataStorage;
                data.DeleteJob("job1");
            }
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
