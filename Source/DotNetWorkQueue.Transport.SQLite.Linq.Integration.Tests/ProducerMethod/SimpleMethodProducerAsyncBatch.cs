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
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Integration.Tests;
using DotNetWorkQueue.Transport.SQLite.Schema;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.ProducerMethod
{
    [Collection("SQLite")]
    public class SimpleMethodProducerAsyncBatch
    {
        [Theory]
        [InlineData(1000, true, true, true, false, false, true, false, false, true, LinqMethodTypes.Dynamic),
         InlineData(1000, false, true, true, false, false, true, false, false, true, LinqMethodTypes.Dynamic),
         InlineData(1000, false, false, false, false, false, false, false, false, true, LinqMethodTypes.Dynamic),
         InlineData(1000, true, false, false, false, false, false, false, false, true, LinqMethodTypes.Dynamic),
         InlineData(1000, false, false, false, false, false, false, true, false, true, LinqMethodTypes.Dynamic),
         InlineData(1000, false, false, false, false, false, true, true, false, true, LinqMethodTypes.Dynamic),
         InlineData(1000, false, true, false, true, true, false, true, false, true, LinqMethodTypes.Dynamic),
         InlineData(1000, false, true, true, true, true, true, true, false, true, LinqMethodTypes.Dynamic),
         InlineData(1000, true, true, true, false, false, true, false, true, true, LinqMethodTypes.Dynamic),
          InlineData(1000, true, true, true, false, false, true, false, false, false, LinqMethodTypes.Dynamic),
         InlineData(1000, false, true, true, false, false, true, false, false, false, LinqMethodTypes.Dynamic),
         InlineData(1000, false, false, false, false, false, false, false, false, false, LinqMethodTypes.Dynamic),
         InlineData(1000, true, false, false, false, false, false, false, false, false, LinqMethodTypes.Dynamic),
         InlineData(1000, false, false, false, false, false, false, true, false, false, LinqMethodTypes.Dynamic),
         InlineData(1000, false, false, false, false, false, true, true, false, false, LinqMethodTypes.Dynamic),
         InlineData(1000, false, true, false, true, true, false, true, false, false, LinqMethodTypes.Dynamic),
         InlineData(1000, false, true, true, true, true, true, true, false, false, LinqMethodTypes.Dynamic),
         InlineData(1000, true, true, true, false, false, true, false, true, false, LinqMethodTypes.Dynamic),
            InlineData(1000, true, true, true, false, false, true, false, false, true, LinqMethodTypes.Compiled),
         InlineData(1000, false, true, true, false, false, true, false, false, true, LinqMethodTypes.Compiled),
         InlineData(1000, false, false, false, false, false, false, false, false, true, LinqMethodTypes.Compiled),
         InlineData(1000, true, false, false, false, false, false, false, false, true, LinqMethodTypes.Compiled),
         InlineData(1000, false, false, false, false, false, false, true, false, true, LinqMethodTypes.Compiled),
         InlineData(1000, false, false, false, false, false, true, true, false, true, LinqMethodTypes.Compiled),
         InlineData(1000, false, true, false, true, true, false, true, false, true, LinqMethodTypes.Compiled),
         InlineData(1000, false, true, true, true, true, true, true, false, true, LinqMethodTypes.Compiled),
         InlineData(1000, true, true, true, false, false, true, false, true, true, LinqMethodTypes.Compiled),
          InlineData(1000, true, true, true, false, false, true, false, false, false, LinqMethodTypes.Compiled),
         InlineData(1000, false, true, true, false, false, true, false, false, false, LinqMethodTypes.Compiled),
         InlineData(1000, false, false, false, false, false, false, false, false, false, LinqMethodTypes.Compiled),
         InlineData(1000, true, false, false, false, false, false, false, false, false, LinqMethodTypes.Compiled),
         InlineData(1000, false, false, false, false, false, false, true, false, false, LinqMethodTypes.Compiled),
         InlineData(1000, false, false, false, false, false, true, true, false, false, LinqMethodTypes.Compiled),
         InlineData(1000, false, true, false, true, true, false, true, false, false, LinqMethodTypes.Compiled),
         InlineData(1000, false, true, true, true, true, true, true, false, false, LinqMethodTypes.Compiled),
         InlineData(1000, true, true, true, false, false, true, false, true, false, LinqMethodTypes.Compiled)]
        public async void Run(
            int messageCount,
            bool interceptors,
            bool enableDelayedProcessing,
            bool enableHeartBeat,
            bool enableMessageExpiration,
            bool enablePriority,
            bool enableStatus,
            bool enableStatusTable,
            bool additionalColumn,
            bool inMemoryDb,
            LinqMethodTypes linqMethodTypes)
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
                            oCreation.Options.EnableDelayedProcessing = enableDelayedProcessing;
                            oCreation.Options.EnableHeartBeat = enableHeartBeat;
                            oCreation.Options.EnableMessageExpiration = enableMessageExpiration;
                            oCreation.Options.EnablePriority = enablePriority;
                            oCreation.Options.EnableStatus = enableStatus;
                            oCreation.Options.EnableStatusTable = enableStatusTable;

                            if (additionalColumn)
                            {
                                oCreation.Options.AdditionalColumns.Add(new Column("OrderID", ColumnTypes.Integer, false, null));
                            }

                            var result = oCreation.CreateQueue();
                            Assert.True(result.Success, result.ErrorMessage);

                            var producer = new ProducerMethodAsyncShared();
                            var id = Guid.NewGuid();
                            await producer.RunTest<SqLiteMessageQueueInit>(queueName,
                                connectionInfo.ConnectionString, interceptors, messageCount, logProvider,
                                Helpers.GenerateData,
                                Helpers.Verify, true, 0, id, linqMethodTypes);
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
