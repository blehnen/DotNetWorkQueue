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
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.ConsumerMethod
{
    [Collection("SQLite")]
    public class ConsumerMethodRollBack
    {
        [Theory]
        [InlineData(500, 0, 240, 5, true, LinqMethodTypes.Dynamic),
        InlineData(50, 5, 200, 10, true, LinqMethodTypes.Dynamic),
        InlineData(10, 15, 180, 7, true, LinqMethodTypes.Dynamic),
        InlineData(500, 0, 240, 5, false, LinqMethodTypes.Dynamic),
        InlineData(50, 5, 200, 10, false, LinqMethodTypes.Dynamic),
        InlineData(10, 15, 180, 7, false, LinqMethodTypes.Dynamic),
        InlineData(10, 45, 200, 10, true, LinqMethodTypes.Dynamic),
        InlineData(10, 45, 220, 7, false, LinqMethodTypes.Dynamic),
            InlineData(500, 0, 240, 5, true, LinqMethodTypes.Compiled),
        InlineData(50, 5, 200, 10, true, LinqMethodTypes.Compiled),
        InlineData(10, 15, 180, 7, true, LinqMethodTypes.Compiled),
        InlineData(500, 0, 240, 5, false, LinqMethodTypes.Compiled),
        InlineData(50, 5, 200, 10, false, LinqMethodTypes.Compiled),
        InlineData(10, 15, 180, 7, false, LinqMethodTypes.Compiled),
        InlineData(10, 45, 200, 10, true, LinqMethodTypes.Compiled),
        InlineData(10, 45, 220, 7, false, LinqMethodTypes.Compiled)]
        public void Run(int messageCount, int runtime, 
            int timeOut, int workerCount, bool inMemoryDb, LinqMethodTypes linqMethodTypes)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (
                    var queueCreator =
                        new QueueCreationContainer<SqLiteMessageQueueInit>(
                            serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    try
                    {

                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueName,
                                    connectionInfo.ConnectionString)
                            )
                        {
                            oCreation.Options.EnableDelayedProcessing = true;
                            oCreation.Options.EnableHeartBeat = true;
                            oCreation.Options.EnableStatus = true;
                            oCreation.Options.EnableStatusTable = true;

                            var result = oCreation.CreateQueue();
                            Assert.True(result.Success, result.ErrorMessage);

                            //create data
                            var producer = new ProducerMethodShared();
                            var id = Guid.NewGuid();
                            if (linqMethodTypes == LinqMethodTypes.Compiled)
                            {
                                producer.RunTestCompiled<SqLiteMessageQueueInit>(queueName,
                               connectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                               Helpers.Verify, false, id, GenerateMethod.CreateRollBackCompiled, runtime);
                            }
                            else
                            {
                                producer.RunTestDynamic<SqLiteMessageQueueInit>(queueName,
                               connectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                               Helpers.Verify, false, id, GenerateMethod.CreateRollBackDynamic, runtime);
                            }

                            //process data
                            var consumer = new ConsumerMethodRollBackShared();
                            consumer.RunConsumer<SqLiteMessageQueueInit>(queueName, connectionInfo.ConnectionString,
                                false,
                                workerCount, logProvider, timeOut, runtime, messageCount,
                                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), id);

                            new VerifyQueueRecordCount(queueName, connectionInfo.ConnectionString, oCreation.Options).Verify(0, false, false);
                            GenerateMethod.ClearRollback(id);
                        }
                    }
                    finally
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
