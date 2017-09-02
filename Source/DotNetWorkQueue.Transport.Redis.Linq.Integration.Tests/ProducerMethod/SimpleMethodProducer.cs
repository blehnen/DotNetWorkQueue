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
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.ProducerMethod
{
    [Collection("Redis")]
    public class SimpleMethodProducer
    {
        [Theory]
        [InlineData(1000, true, false, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(1000, false, false, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(5000, true, false, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(5000, false, false, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(1000, true, true, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(1000, false, true, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(1000, true, false, true, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(1000, false, false, false, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(1000, true, false, true, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(1000, false, false, false, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(1000, true, true, true, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(1000, false, true, true, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(1000, true, false, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
         InlineData(1000, false, false, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
         InlineData(5000, true, false, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
         InlineData(5000, false, false, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
         InlineData(1000, true, true, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
         InlineData(1000, false, true, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
         InlineData(1000, true, false, true, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
         InlineData(1000, false, false, false, true, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
         InlineData(1000, true, false, true, true, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
         InlineData(1000, false, false, false, true, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
         InlineData(1000, true, true, true, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
         InlineData(1000, false, true, true, true, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),

         InlineData(1000, true, false, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(1000, false, false, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(5000, true, false, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(5000, false, false, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(1000, true, true, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(1000, false, true, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(1000, true, false, true, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(1000, false, false, false, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(1000, true, false, true, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(1000, false, false, false, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(1000, true, true, true, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(1000, false, true, true, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(1000, true, false, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
         InlineData(1000, false, false, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
         InlineData(5000, true, false, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
         InlineData(5000, false, false, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
         InlineData(1000, true, true, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
         InlineData(1000, false, true, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
         InlineData(1000, true, false, true, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
         InlineData(1000, false, false, false, true, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
         InlineData(1000, true, false, true, true, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
         InlineData(1000, false, false, false, true, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
         InlineData(1000, true, true, true, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
         InlineData(1000, false, true, true, true, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled)]
        public void Run(
            int messageCount,
            bool interceptors,
            bool batchSending,
            bool enableDelay,
            bool enableExpiration,
            ConnectionInfoTypes type,
             LinqMethodTypes linqMethodTypes)
        {

            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            var producer = new ProducerMethodShared();
            var connectionString = new ConnectionInfo(type).ConnectionString;
            using (
                var queueCreator =
                    new QueueCreationContainer<RedisQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                try
                {
                    var id = Guid.NewGuid();
                    if (enableExpiration && enableDelay)
                    {
                        if (linqMethodTypes == LinqMethodTypes.Compiled)
                        {
                            producer.RunTestCompiled<RedisQueueInit>(queueName,
                                connectionString, interceptors, messageCount, logProvider,
                                Helpers.GenerateDelayExpiredData,
                                Helpers.Verify, batchSending, id, GenerateMethod.CreateCompiled, 0, null);
                        }
                        else
                        {
                            producer.RunTestDynamic<RedisQueueInit>(queueName,
                               connectionString, interceptors, messageCount, logProvider,
                               Helpers.GenerateDelayExpiredData,
                               Helpers.Verify, batchSending, id, GenerateMethod.CreateDynamic, 0, null);
                        }
                    }
                    else if (enableDelay)
                    {
                        if (linqMethodTypes == LinqMethodTypes.Compiled)
                        {
                            producer.RunTestCompiled<RedisQueueInit>(queueName,
                                connectionString, interceptors, messageCount, logProvider, Helpers.GenerateDelayData,
                                Helpers.Verify, batchSending, id, GenerateMethod.CreateCompiled, 0, null);
                        }
                        else
                        {
                            producer.RunTestDynamic<RedisQueueInit>(queueName,
                               connectionString, interceptors, messageCount, logProvider, Helpers.GenerateDelayData,
                               Helpers.Verify, batchSending, id, GenerateMethod.CreateDynamic, 0, null);
                        }
                    }
                    else if (enableExpiration)
                    {
                        if (linqMethodTypes == LinqMethodTypes.Compiled)
                        {
                            producer.RunTestCompiled<RedisQueueInit>(queueName,
                                connectionString, interceptors, messageCount, logProvider, Helpers.GenerateExpiredData,
                                Helpers.Verify, batchSending, id, GenerateMethod.CreateCompiled, 0, null);
                        }
                        else
                        {
                            producer.RunTestDynamic<RedisQueueInit>(queueName,
                               connectionString, interceptors, messageCount, logProvider, Helpers.GenerateExpiredData,
                               Helpers.Verify, batchSending, id, GenerateMethod.CreateDynamic, 0, null);
                        }
                    }
                    else
                    {
                        if (linqMethodTypes == LinqMethodTypes.Compiled)
                        {
                            producer.RunTestCompiled<RedisQueueInit>(queueName,
                                connectionString, interceptors, messageCount, logProvider, Helpers.GenerateData,
                                Helpers.Verify, batchSending, id, GenerateMethod.CreateCompiled, 0, null);
                        }
                        else
                        {
                            producer.RunTestDynamic<RedisQueueInit>(queueName,
                               connectionString, interceptors, messageCount, logProvider, Helpers.GenerateData,
                               Helpers.Verify, batchSending, id, GenerateMethod.CreateDynamic, 0, null);
                        }
                    }
                }
                finally
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
