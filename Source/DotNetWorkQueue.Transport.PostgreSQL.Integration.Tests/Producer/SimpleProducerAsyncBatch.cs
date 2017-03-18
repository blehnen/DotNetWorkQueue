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
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Schema;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Producer
{
    [Collection("postgresql")]
    public class SimpleProducerAsyncBatch
    {
        [Theory]
        [InlineData(1000, true, true, true, false, false, false, true, false, false),
         InlineData(1000, false, true, true, false, false, false, true, false, false),
         InlineData(1000, false, false, false, false, false, false, false, false, false),
         InlineData(1000, true, false, false, false, false, false, false, false, false),
         InlineData(1000, false, false, false, false, false, false, false, true, false),
         InlineData(1000, false, false, false, false, false, false, true, true, false),
         InlineData(1000, false, true, false, true, true, true, false, true, false),
         InlineData(1000, false, true, true, false, true, true, true, true, false),
            InlineData(1000, true, true, true, false, false, false, true, false, true)]
        public async void Run(
            int messageCount,
            bool interceptors,
            bool enableDelayedProcessing,
            bool enableHeartBeat,
            bool enableHoldTransactionUntilMessageCommited,
            bool enableMessageExpiration,
            bool enablePriority,
            bool enableStatus,
            bool enableStatusTable,
            bool additionalColumn)
        {

            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            using (
                var queueCreator =
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
                        oCreation.Options.EnableHoldTransactionUntilMessageCommited =
                            enableHoldTransactionUntilMessageCommited;
                        oCreation.Options.EnablePriority = enablePriority;
                        oCreation.Options.EnableStatus = enableStatus;
                        oCreation.Options.EnableStatusTable = enableStatusTable;

                        if (additionalColumn)
                        {
                            oCreation.Options.AdditionalColumns.Add(new Column("OrderID", ColumnTypes.Integer, false));
                        }

                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);

                        var producer = new ProducerAsyncShared();
                        await producer.RunTest<PostgreSqlMessageQueueInit, FakeMessage>(queueName,
                            ConnectionInfo.ConnectionString, interceptors, messageCount, logProvider,
                            Helpers.GenerateData,
                            Helpers.Verify, true);
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
