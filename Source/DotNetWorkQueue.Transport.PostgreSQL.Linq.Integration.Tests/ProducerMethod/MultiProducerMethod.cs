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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.ProducerMethod
{
    [Collection("PostgreSQL")]
    public class MultiProducerMethod
    {
        [Theory]
        [InlineData(LinqMethodTypes.Dynamic),
         InlineData(LinqMethodTypes.Compiled)]
        public void Run(LinqMethodTypes linqMethodTypes)
        {
            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            using (var queueCreator =
                new QueueCreationContainer<PostgreSqlMessageQueueInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                try
                {

                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueName,
                                ConnectionInfo.ConnectionString)
                        )
                    {
                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);

                        RunTest(queueName, 1000, 10, logProvider, Guid.NewGuid(), 0, linqMethodTypes);
                        LoggerShared.CheckForErrors(queueName);
                        new VerifyQueueData(queueName, oCreation.Options).Verify(1000*10, null);
                    }
                }
                finally
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

        private void RunTest(string queueName, int messageCount, int queueCount, ILogProvider logProvider, Guid id, int runTime, LinqMethodTypes linqMethodTypes)
        {
            var tasks = new List<Task>(queueCount);
            for (var i = 0; i < queueCount; i++)
            {
                var producer = new ProducerMethodShared();
                switch (linqMethodTypes)
                {
                    case LinqMethodTypes.Dynamic:
                        tasks.Add(
                            new Task(
                                () =>
                                    producer.RunTestDynamic<PostgreSqlMessageQueueInit>(queueName,
                                        ConnectionInfo.ConnectionString, false, messageCount,
                                        logProvider, Helpers.GenerateData, Helpers.NoVerification, true, false, id,
                                        GenerateMethod.CreateDynamic, runTime)));
                        break;
                    case LinqMethodTypes.Compiled:
                        tasks.Add(
                            new Task(
                                () =>
                                    producer.RunTestCompiled<PostgreSqlMessageQueueInit>(queueName,
                                        ConnectionInfo.ConnectionString, false, messageCount,
                                        logProvider, Helpers.GenerateData, Helpers.NoVerification, true, false, id,
                                        GenerateMethod.CreateCompiled, runTime)));
                        break;
                }
            }
            tasks.AsParallel().ForAll(x => x.Start());
            Task.WaitAll(tasks.ToArray());
        }
    }
}
