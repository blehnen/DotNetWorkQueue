#region Using

using System;
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

#endregion

namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests
{
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
                Assert.Equal(recordCount, count);
            }
            else
            {
                var count = dataStorage.GetDequeueCount();
                Assert.Equal(recordCount, count);
            }
        }
    }
    public class VerifyErrorCounts
    {
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        public void Verify(ICreationScope scope, long messageCount, int errorCount)
        {
            var realScope = (CreationScope)scope;
            if (realScope.ContainedClears.TryPeek(out var dataStorage))
            {
                var data = (IDataStorage)dataStorage;
                var errors = data.GetErrorCount();
                Assert.Equal(messageCount, errors);
            }
        }
    }
}
