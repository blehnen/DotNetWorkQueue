using System;
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Memory;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests
{
    public static class Helpers
    {
        public static void Verify(QueueConnection notUsed, QueueProducerConfiguration config, long recordCount, ICreationScope scope)
        {
            var realScope = (CreationScope)scope;
            if (realScope.ContainedClears.TryPeek(out var dataStorage))
            {
                var data = (IDataStorage)dataStorage;
                var messageCount = data.RecordCount;
                Assert.AreEqual(recordCount, messageCount);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static void NoVerification(QueueConnection queueConnection, QueueProducerConfiguration queueProducerConfiguration, long messageCount, ICreationScope scope)
        {
        }

        public static AdditionalMessageData GenerateData(QueueProducerConfiguration configuration)
        {
            return null;
        }
    }

    public class VerifyQueueRecordCount
    {
        public void Verify(ICreationScope scope, int recordCount, bool existingCount)
        {
            var realScope = (CreationScope)scope;
            if (realScope.ContainedClears.TryPeek(out var dataStorage))
            {
                var data = (IDataStorage)dataStorage;
                AllTablesRecordCount(data, recordCount, existingCount);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query Ok")]
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private void AllTablesRecordCount(IDataStorage dataStorage, int recordCount, bool existingCount)
        {
            if (existingCount)
            {
                var count = dataStorage.RecordCount;
                Assert.AreEqual(recordCount, count);
            }
            else
            {
                var count = dataStorage.GetDequeueCount();
                Assert.AreEqual(recordCount, count);
            }
        }
    }
}
