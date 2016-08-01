// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.JobScheduler
{
    [Collection("Redis")]
    public class JobSchedulerMultipleTests
    {
        [Theory]
        [InlineData(true, 10, ConnectionInfoTypes.Linux),
      InlineData(false, 10, ConnectionInfoTypes.Linux),
      InlineData(true, 10, ConnectionInfoTypes.Windows),
      InlineData(false, 10, ConnectionInfoTypes.Windows)]
        public void RunMultiple(
           bool interceptors,
           int producerCount,
           ConnectionInfoTypes type)
        {
            var queueName = GenerateQueueName.Create();
            var connectionString = new ConnectionInfo(type).ConnectionString;
            using (var queueContainer = new QueueContainer<RedisQueueInit>(x => {
            }))
            {
                try
                {
                    var tests = new JobSchedulerTestsShared();
                    tests.RunTestMultipleProducers<RedisQueueInit, RedisJobQueueCreation>(queueName,
                        connectionString, interceptors, producerCount, queueContainer.CreateTimeSync(connectionString));
                }
                finally
                {

                    using (var queueCreator =
                        new QueueCreationContainer<RedisQueueInit>())
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<RedisQueueCreation>(queueName,
                                    connectionString)
                            )
                        {
                            oCreation.RemoveQueue();
                        }
                    }
                }
            }
        }
    }
}
