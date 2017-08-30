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
            if (realScope.ContainedClears.TryPeek(out IClear dataStorage))
            {
                var data = (IDataStorage)(dataStorage);
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
            if (realScope.ContainedClears.TryPeek(out IClear dataStorage))
            {
                var data = (IDataStorage)(dataStorage);
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
            if (realScope.ContainedClears.TryPeek(out IClear dataStorage))
            {
                var data = (IDataStorage) (dataStorage);
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
