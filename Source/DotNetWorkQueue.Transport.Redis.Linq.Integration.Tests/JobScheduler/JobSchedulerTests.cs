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

using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.JobScheduler
{
    [Collection("Redis")]
    public class JobSchedulerTests
    {
        [Theory]
        [InlineData(false, ConnectionInfoTypes.Linux),
         InlineData(true, ConnectionInfoTypes.Linux),
         InlineData(false, ConnectionInfoTypes.Windows),
         InlineData(true, ConnectionInfoTypes.Windows)]
        public void Run(
            bool dynamic,
            ConnectionInfoTypes type)
        {

            var queueName = GenerateQueueName.Create();
            var connectionString = new ConnectionInfo(type).ConnectionString;
            using (var queueContainer = new QueueContainer<RedisQueueInit>(x => { }))
            {
                try
                {
                    var tests = new JobSchedulerTestsShared();
                    if (!dynamic)
                    {
                        tests.RunEnqueueTestCompiled<RedisQueueInit, RedisJobQueueCreation>(queueName,
                            connectionString, true,
                            Helpers.Verify, Helpers.SetError, queueContainer.CreateTimeSync(connectionString), null);
                    }
                    else
                    {
                        tests.RunEnqueueTestDynamic<RedisQueueInit, RedisJobQueueCreation>(queueName,
                            connectionString, true,
                            Helpers.Verify, Helpers.SetError, queueContainer.CreateTimeSync(connectionString), null);
                    }
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
