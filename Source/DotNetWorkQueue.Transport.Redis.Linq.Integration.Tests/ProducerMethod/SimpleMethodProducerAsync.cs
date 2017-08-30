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
    public class SimpleMethodProducerAsync
    {
        [Theory]
        [InlineData(1000, true, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
        InlineData(1000, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
        InlineData(1000, true, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
        InlineData(1000, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
        InlineData(1000, true, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
        InlineData(1000, false, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
        InlineData(1000, true, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
        InlineData(1000, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
        InlineData(1000, true, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
        InlineData(1000, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
        InlineData(1000, true, true, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
        InlineData(1000, false, true, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
        InlineData(1000, true, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
        InlineData(1000, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
        InlineData(1000, true, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
        InlineData(1000, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
        InlineData(1000, true, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
        InlineData(1000, false, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
        InlineData(1000, true, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
        InlineData(1000, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
        InlineData(1000, true, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
        InlineData(1000, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
        InlineData(1000, true, true, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
        InlineData(1000, false, true, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled)]
        public async void Run(
           int messageCount,
           bool interceptors,
           bool batchSending,
           ConnectionInfoTypes type,
           LinqMethodTypes linqMethodTypes)
        {

            var id = Guid.NewGuid();
            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            var producer = new ProducerMethodAsyncShared();
            var connectionString = new ConnectionInfo(type).ConnectionString;
            using (
                var queueCreator =
                    new QueueCreationContainer<RedisQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                try
                {
                    await producer.RunTestAsync<RedisQueueInit>(queueName,
                        connectionString, interceptors, messageCount, logProvider, Helpers.GenerateData,
                        Helpers.Verify, batchSending, 0, id, linqMethodTypes, null).ConfigureAwait(false);
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
