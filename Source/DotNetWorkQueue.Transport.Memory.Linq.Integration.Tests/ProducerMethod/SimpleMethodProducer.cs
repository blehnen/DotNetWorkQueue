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
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.ProducerMethod
{
    public class SimpleMethodProducer
    {
        [Theory]
#if NETFULL
        [InlineData(1000, LinqMethodTypes.Dynamic),
         InlineData(100, LinqMethodTypes.Compiled)]
#else
        [InlineData(100, LinqMethodTypes.Compiled)]
#endif
        public void Run(
            int messageCount,
            LinqMethodTypes linqMethodTypes)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (var queueCreator =
                    new QueueCreationContainer<MemoryMessageQueueInit>(
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

                            var id = Guid.NewGuid();
                            var producer = new ProducerMethodShared();
                            if (linqMethodTypes == LinqMethodTypes.Compiled)
                            {
                                producer.RunTestCompiled<MemoryMessageQueueInit>(queueName,
                               connectionInfo.ConnectionString, true, messageCount, logProvider,
                               Helpers.GenerateData,
                               Helpers.Verify, false, id, GenerateMethod.CreateCompiled, 0, oCreation.Scope);
                            }
#if NETFULL
                            else
                            {
                                producer.RunTestDynamic<MemoryMessageQueueInit>(queueName,
                               connectionInfo.ConnectionString, true, messageCount, logProvider,
                               Helpers.GenerateData,
                               Helpers.Verify, false, id, GenerateMethod.CreateDynamic, 0, oCreation.Scope);
                            }
#endif
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
    }
}
