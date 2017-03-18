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
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.JobScheduler
{
    [Collection("PostgreSQL")]
    public class JobSchedulerMultipleTests
    {
        [Theory]
        [InlineData(true, 10),
         InlineData(false, 10)]
        public void Run(
            bool interceptors,
            int producerCount)
        {
            var queueName = GenerateQueueName.Create();
            using (var queueContainer = new QueueContainer<PostgreSqlMessageQueueInit>(x => {
            }))
            {
                try
                {
                    var tests = new JobSchedulerTestsShared();
                    tests.RunTestMultipleProducers<PostgreSqlMessageQueueInit, PostgreSqlJobQueueCreation>(queueName,
                        ConnectionInfo.ConnectionString, interceptors, producerCount, queueContainer.CreateTimeSync(ConnectionInfo.ConnectionString));
                }
                finally
                {

                    using (var queueCreator =
                        new QueueCreationContainer<PostgreSqlMessageQueueInit>())
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueName,
                                    ConnectionInfo.ConnectionString)
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
