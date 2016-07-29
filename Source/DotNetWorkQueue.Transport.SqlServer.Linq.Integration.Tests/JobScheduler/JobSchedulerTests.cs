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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.JobScheduler
{
    [Collection("JobScheduler")]
    public class JobSchedulerTests
    {
        [Theory]
        [InlineData(true, false),
         InlineData(false, false),
         InlineData(true, true),
         InlineData(false, true)]
        public void Run(
            bool interceptors,
            bool dynamic)
        {
            var queueName = GenerateQueueName.Create();
            try
            {
                var tests = new JobSchedulerTestsShared();
                if (!dynamic)
                {
                    tests.RunEnqueueTestCompiled<SqlServerMessageQueueInit, SqlServerJobQueueCreation>(queueName,
                        ConnectionInfo.ConnectionString, interceptors,
                        Helpers.Verify, Helpers.SetError);
                }
                else
                {
                    tests.RunEnqueueTestDynamic<SqlServerMessageQueueInit, SqlServerJobQueueCreation>(queueName,
                       ConnectionInfo.ConnectionString, interceptors,
                       Helpers.Verify, Helpers.SetError);
                }
            }
            finally
            {

                using (var queueCreator =
                    new QueueCreationContainer<SqlServerMessageQueueInit>())
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueName,
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
