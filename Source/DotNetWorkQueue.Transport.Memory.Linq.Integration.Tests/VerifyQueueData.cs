// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
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
