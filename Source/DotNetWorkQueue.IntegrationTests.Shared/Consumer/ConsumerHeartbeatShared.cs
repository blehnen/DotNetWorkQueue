using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Interceptors;
using DotNetWorkQueue.Logging;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.IntegrationTests.Shared.Consumer
{
    public class ConsumerHeartBeatShared<TMessage>
        where TMessage : class
    {
        public void RunConsumer<TTransportInit>(QueueConnection queueConnection, bool addInterceptors,
            ILogger logProvider,
            int runTime, int messageCount,
            int workerCount, int timeOut,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime,
            string updateTime,
            string route,
            bool enableChaos,
            ICreationScope scope)
            where TTransportInit : ITransportInit, new()
        {
            if (enableChaos)
                timeOut *= 2;

            var queue = new ConsumerCancelWorkShared<TTransportInit, TMessage>();
            if (addInterceptors)
            {
                queue.RunConsumer(queueConnection, true, logProvider, runTime, messageCount, workerCount, timeOut,
                    serviceRegister => serviceRegister.Register<IRollbackMessage, MessageProcessingFailRollBack>(LifeStyles.Singleton).RegisterCollection<IMessageInterceptor>(new[]
                        {
                            typeof (GZipMessageInterceptor), //gzip compression
                            typeof (TripleDesMessageInterceptor) //encryption
                        }).Register(() => new TripleDesMessageInterceptorConfiguration(Convert.FromBase64String("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                Convert.FromBase64String("aaaaaaaaaaa=")), LifeStyles.Singleton).RegisterNonScopedSingleton(scope),
                    heartBeatTime, heartBeatMonitorTime, updateTime, route, enableChaos, scope);
            }
            else
            {
                queue.RunConsumer(queueConnection, false, logProvider, runTime, messageCount, workerCount, timeOut,
                    serviceRegister => serviceRegister.Register<IRollbackMessage, MessageProcessingFailRollBack>(LifeStyles.Singleton).RegisterNonScopedSingleton(scope),
                    heartBeatTime, heartBeatMonitorTime, updateTime, route, enableChaos, scope);
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
