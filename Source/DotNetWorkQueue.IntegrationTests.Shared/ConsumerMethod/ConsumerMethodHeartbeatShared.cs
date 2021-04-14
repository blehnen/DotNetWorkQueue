using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Interceptors;
using DotNetWorkQueue.Logging;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod
{
    public class ConsumerMethodHeartBeatShared
    {
        public void RunConsumer<TTransportInit>(QueueConnection queueConnection, bool addInterceptors,
            ILogger logProvider,
            int runTime, int messageCount,
            int workerCount, int timeOut,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime, Guid id, string updateTime, bool enableChaos, ICreationScope scope)
            where TTransportInit : ITransportInit, new()
        {
            if (enableChaos)
                timeOut *= 2;

            var queue = new ConsumerMethodCancelWorkShared<TTransportInit>();
            if (addInterceptors)
            {
                queue.RunConsumer(queueConnection, true, logProvider, runTime, messageCount, workerCount, timeOut,
                    serviceRegister => serviceRegister.Register<IRollbackMessage, MessageProcessingFailRollBack>(LifeStyles.Singleton).Register<IMessageMethodHandling>(() => new MethodMessageProcessingCancel(id), LifeStyles.Singleton).RegisterCollection<IMessageInterceptor>(new[]
                        {
                            typeof (GZipMessageInterceptor), //gzip compression
                            typeof (TripleDesMessageInterceptor) //encryption
                        }).Register(() => new TripleDesMessageInterceptorConfiguration(Convert.FromBase64String("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                Convert.FromBase64String("aaaaaaaaaaa=")), LifeStyles.Singleton),
                    heartBeatTime, heartBeatMonitorTime, updateTime, id, enableChaos, scope);
            }
            else
            {
                queue.RunConsumer(queueConnection, false, logProvider, runTime, messageCount, workerCount, timeOut,
                    serviceRegister => serviceRegister.Register<IRollbackMessage, MessageProcessingFailRollBack>(LifeStyles.Singleton).Register<IMessageMethodHandling>(() => new MethodMessageProcessingCancel(id), LifeStyles.Singleton),
                    heartBeatTime, heartBeatMonitorTime, updateTime, id, enableChaos, scope);
            }
        }

        internal class MessageProcessingFailRollBack : IRollbackMessage
        {
            public bool Rollback(IMessageContext context)
            {
                return true; //don't really process rollback
            }
        }
    }
}
