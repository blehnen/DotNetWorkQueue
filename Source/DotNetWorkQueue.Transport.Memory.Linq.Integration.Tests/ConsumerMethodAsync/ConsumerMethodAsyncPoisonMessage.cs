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
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.Memory.Basic;
using DotNetWorkQueue.Transport.SQLite.Integration.Tests;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.ConsumerMethodAsync
{
    public class ConsumerMethodAsyncPoisonMessage
    {
        [Theory]
        [InlineData(1, 20, 1, 1, 0, LinqMethodTypes.Dynamic),
         InlineData(10, 30, 5, 1, 0, LinqMethodTypes.Compiled)]
        public void Run(int messageCount, int timeOut, int workerCount, 
            int readerCount, int queueSize, LinqMethodTypes linqMethodTypes)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (var queueCreator =
                    new QueueCreationContainer<MessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    try
                    {

                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<MessageQueueCreation>(queueName,
                                    connectionInfo.ConnectionString)
                            )
                        {
                            var result = oCreation.CreateQueue();
                            Assert.True(result.Success, result.ErrorMessage);

                            //create data
                            var producer = new ProducerMethodShared();
                            var id = Guid.NewGuid();
                            if (linqMethodTypes == LinqMethodTypes.Compiled)
                            {
                                producer.RunTestCompiled<MessageQueueInit>(queueName,
                                    connectionInfo.ConnectionString, false, messageCount, logProvider,
                                    Helpers.GenerateData,
                                    Helpers.Verify, false, id, GenerateMethod.CreateNoOpCompiled, 0, oCreation.Scope);
                            }
                            else
                            {
                                producer.RunTestDynamic<MessageQueueInit>(queueName,
                                   connectionInfo.ConnectionString, false, messageCount, logProvider,
                                   Helpers.GenerateData,
                                   Helpers.Verify, false, id, GenerateMethod.CreateNoOpDynamic, 0, oCreation.Scope);
                            }

                            //process data
                            var consumer = new ConsumerMethodAsyncPoisonMessageShared();
                            consumer.RunConsumer<MessageQueueInit>(queueName, connectionInfo.ConnectionString,
                                false,
                                workerCount, logProvider,
                                timeOut, readerCount, queueSize, messageCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35));

                            ValidateErrorCounts(oCreation.Scope, messageCount);
                        }
                    }
                    finally
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<MessageQueueCreation>(queueName,
                                    connectionInfo.ConnectionString)
                            )
                        {
                            oCreation.RemoveQueue();
                        }
                    }
                }
            }
        }

        private void ValidateErrorCounts(ICreationScope scope, long messageCount)
        {
            //poison messages are moved to the error queue right away
            //they don't update the tracking table, so specify 0 for the error count.
            //They still update the error table itself
            new VerifyErrorCounts().Verify(scope, messageCount, 0);
        }
    }
}
