// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Xunit;
namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.ConsumerAsync
{
    [Collection("ConsumerAsync Tests")]
    public class ConsumerAsyncRollBack
    {
        [Theory]
        [InlineData(100, 1, 400, 5, 5, 5, false),
         InlineData(50, 5, 200, 5, 1, 3, false),
         InlineData(10, 5, 180, 7, 1, 1, false),
         InlineData(100, 1, 400, 5, 5, 5, true),
         InlineData(50, 5, 200, 5, 1, 3, true),
         InlineData(10, 5, 180, 7, 1, 1, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
            bool useTransactions)
        {
            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName);
            using (var queueCreator =
                new QueueCreationContainer<SqlServerMessageQueueInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                try
                {

                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueName,
                                ConnectionInfo.ConnectionString)
                        )
                    {
                        oCreation.Options.EnableDelayedProcessing = true;
                        oCreation.Options.EnableHeartBeat = !useTransactions;
                        oCreation.Options.EnableHoldTransactionUntilMessageCommited = useTransactions;
                        oCreation.Options.EnableStatus = !useTransactions;
                        oCreation.Options.EnableStatusTable = true;

                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);

                        //create data
                        var producer = new ProducerShared();
                        producer.RunTest<SqlServerMessageQueueInit, FakeMessage>(queueName,
                            ConnectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                            Helpers.Verify, false);

                        //process data
                        var consumer = new ConsumerAsyncRollBackShared<FakeMessage>();
                        consumer.RunConsumer<SqlServerMessageQueueInit>(queueName, ConnectionInfo.ConnectionString,
                            false,
                            workerCount, logProvider,
                            timeOut, readerCount, queueSize, runtime, messageCount);
                        LoggerShared.CheckForErrors(queueName);
                        new VerifyQueueRecordCount(queueName, oCreation.Options).Verify(0, false, false);
                    }
                }
                finally
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
