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
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;
using DotNetWorkQueue.Transport.PostgreSQL.Schema;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.ProducerMethod
{
    [Collection("PostgreSQL")]
    public class SimpleProducerMethodBatch
    {
        [Theory]
        [InlineData(1000, true, true, true, false, false, false, true, false, false, LinqMethodTypes.Dynamic),
         InlineData(1000, false, true, true, false, false, false, true, false, false, LinqMethodTypes.Dynamic),
         InlineData(1000, false, false, false, false, false, false, false, false, false, LinqMethodTypes.Dynamic),
         InlineData(1000, true, false, false, false, false, false, false, false, false, LinqMethodTypes.Dynamic),
         InlineData(1000, false, false, false, false, false, false, false, true, false, LinqMethodTypes.Dynamic),
         InlineData(1000, false, false, false, false, false, false, true, true, false, LinqMethodTypes.Dynamic),
         InlineData(1000, false, true, false, true, true, true, false, true, false, LinqMethodTypes.Dynamic),
         InlineData(5000, false, true, true, false, true, true, true, true, false, LinqMethodTypes.Dynamic),
         InlineData(5000, true, true, true, false, false, false, true, false, true, LinqMethodTypes.Dynamic),
            InlineData(1000, true, true, true, false, false, false, true, false, false, LinqMethodTypes.Compiled),
         InlineData(1000, false, true, true, false, false, false, true, false, false, LinqMethodTypes.Compiled),
         InlineData(1000, false, false, false, false, false, false, false, false, false, LinqMethodTypes.Compiled),
         InlineData(1000, true, false, false, false, false, false, false, false, false, LinqMethodTypes.Compiled),
         InlineData(1000, false, false, false, false, false, false, false, true, false, LinqMethodTypes.Compiled),
         InlineData(1000, false, false, false, false, false, false, true, true, false, LinqMethodTypes.Compiled),
         InlineData(1000, false, true, false, true, true, true, false, true, false, LinqMethodTypes.Compiled),
         InlineData(5000, false, true, true, false, true, true, true, true, false, LinqMethodTypes.Compiled),
         InlineData(5000, true, true, true, false, false, false, true, false, true, LinqMethodTypes.Compiled)]
        public void Run(
            int messageCount,
            bool interceptors,
            bool enableDelayedProcessing,
            bool enableHeartBeat,
            bool enableHoldTransactionUntilMessageCommited,
            bool enableMessageExpiration,
            bool enablePriority,
            bool enableStatus,
            bool enableStatusTable,
            bool additionalColumn,
            LinqMethodTypes linqMethodTypes)
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
                        oCreation.Options.EnableDelayedProcessing = enableDelayedProcessing;
                        oCreation.Options.EnableHeartBeat = enableHeartBeat;
                        oCreation.Options.EnableMessageExpiration = enableMessageExpiration;
                        oCreation.Options.EnableHoldTransactionUntilMessageCommited = enableHoldTransactionUntilMessageCommited;
                        oCreation.Options.EnablePriority = enablePriority;
                        oCreation.Options.EnableStatus = enableStatus;
                        oCreation.Options.EnableStatusTable = enableStatusTable;

                        if (additionalColumn)
                        {
                            oCreation.Options.AdditionalColumns.Add(new Column("OrderID", ColumnTypes.Integer, false));
                        }

                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);

                        var producer = new ProducerMethodShared();
                        if (linqMethodTypes == LinqMethodTypes.Compiled)
                        {
                            producer.RunTestCompiled<PostgreSqlMessageQueueInit>(queueName,
                           ConnectionInfo.ConnectionString, interceptors, messageCount, logProvider,
                           Helpers.GenerateData,
                           Helpers.Verify, true, Guid.NewGuid(), GenerateMethod.CreateCompiled, 0);
                        }
                        else
                        {
                            producer.RunTestDynamic<PostgreSqlMessageQueueInit>(queueName,
                           ConnectionInfo.ConnectionString, interceptors, messageCount, logProvider,
                           Helpers.GenerateData,
                           Helpers.Verify, true, Guid.NewGuid(), GenerateMethod.CreateDynamic, 0);
                        }
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
    }
}
