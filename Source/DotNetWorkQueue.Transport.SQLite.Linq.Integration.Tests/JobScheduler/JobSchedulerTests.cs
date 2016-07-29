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
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Integration.Tests;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.JobScheduler
{
    [Collection("SQLite")]
    public class JobSchedulerTests
    {
        [Theory]
        [InlineData(true, false, false),
         InlineData(false, false, false),
         InlineData(true, true, false),
         InlineData(false, true, false),

         InlineData(true, false, true),
         InlineData(false, false, true),
         InlineData(true, true, true),
         InlineData(false, true, true)]
        public void Run(
            bool interceptors,
            bool dynamic,
            bool inMemoryDb)
        {
            using (var queueContainer = new QueueContainer<SqLiteMessageQueueInit>(x => {}))
            {
                using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
                {
                    var queueName = GenerateQueueName.Create();
                    try
                    {
                        var tests = new JobSchedulerTestsShared();
                        if (!dynamic)
                        {
                            tests.RunEnqueueTestCompiled<SqLiteMessageQueueInit, SqliteJobQueueCreation>(queueName,
                                connectionInfo.ConnectionString, interceptors,
                                Helpers.Verify, Helpers.SetError, queueContainer.CreateTimeSync(connectionInfo.ConnectionString));
                        }
                        else
                        {
                            tests.RunEnqueueTestDynamic<SqLiteMessageQueueInit, SqliteJobQueueCreation>(queueName,
                                connectionInfo.ConnectionString, interceptors,
                                Helpers.Verify, Helpers.SetError, queueContainer.CreateTimeSync(connectionInfo.ConnectionString));
                        }
                    }
                    finally
                    {

                        using (var queueCreator =
                            new QueueCreationContainer<SqLiteMessageQueueInit>())
                        {
                            using (
                                var oCreation =
                                    queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueName,
                                        connectionInfo.ConnectionString)
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
}
