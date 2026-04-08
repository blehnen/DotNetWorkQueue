using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.ProducerMethod
{
    [TestClass]
    public class MultiMethodProducer
    {
        [TestMethod]
        public void Run()
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod.Implementation.MultiMethodProducer();

                consumer.Run<MemoryMessageQueueInit, MessageQueueCreation>(new QueueConnection(queueName,
                        connectionInfo.ConnectionString), 100, 10, false,
                    Helpers.GenerateData, VerifyQueueCount);
            }
        }

        private void VerifyQueueCount(QueueConnection arg1, IBaseTransportOptions arg2, ICreationScope arg3, int arg4, string arg5)
        {
            //noop
        }
    }
}
