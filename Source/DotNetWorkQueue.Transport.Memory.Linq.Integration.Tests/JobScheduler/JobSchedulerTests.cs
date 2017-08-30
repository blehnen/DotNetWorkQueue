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
using DotNetWorkQueue.Transport.Memory.Basic;
using DotNetWorkQueue.Transport.SQLite.Integration.Tests;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.JobScheduler
{
    public class JobSchedulerTests
    {
        [Theory]
        [InlineData(false),
         InlineData(true)]
        public void Run(
            bool dynamic)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                using (var queueCreator =
                    new QueueCreationContainer<MessageQueueInit>())
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<MessageQueueCreation>(queueName,
                                connectionInfo.ConnectionString)
                    )
                    {
                        using (var queueContainer = new QueueContainer<MessageQueueInit>(x => { }))
                        {
                            try
                            {
                                var tests = new JobSchedulerTestsShared();
                                if (!dynamic)
                                {
                                    tests.RunEnqueueTestCompiled<MessageQueueInit, JobQueueCreation>(
                                        queueName,
                                        connectionInfo.ConnectionString, true,
                                        Helpers.Verify, Helpers.SetError,
                                        queueContainer.CreateTimeSync(connectionInfo.ConnectionString),
                                            oCreation.Scope);
                                }
                                else
                                {
                                    tests.RunEnqueueTestDynamic<MessageQueueInit, JobQueueCreation>(
                                        queueName,
                                        connectionInfo.ConnectionString, true,
                                        Helpers.Verify, Helpers.SetError,
                                        queueContainer.CreateTimeSync(connectionInfo.ConnectionString),
                                            oCreation.Scope);
                                }
                            }
                            finally
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
