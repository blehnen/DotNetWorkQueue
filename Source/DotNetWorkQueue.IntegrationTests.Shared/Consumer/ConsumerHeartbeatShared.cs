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
using DotNetWorkQueue.Interceptors;
using DotNetWorkQueue.Logging;
namespace DotNetWorkQueue.IntegrationTests.Shared.Consumer
{
    public class ConsumerHeartBeatShared<TMessage>
        where TMessage : class
    {
        public void RunConsumer<TTransportInit>(string queueName, string connectionString, bool addInterceptors,
            ILogProvider logProvider,
            int runTime, int messageCount,
            int workerCount, int timeOut,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime,
            string route = null)
            where TTransportInit : ITransportInit, new()
        {
            var queue = new ConsumerCancelWorkShared<TTransportInit, TMessage>();
            if (addInterceptors)
            {
                queue.RunConsumer(queueName, connectionString, true, logProvider, runTime, messageCount, workerCount, timeOut,
                    serviceRegister => serviceRegister.Register<IRollbackMessage, MessageProcessingFailRollBack>(LifeStyles.Singleton).RegisterCollection<IMessageInterceptor>(new[]
                        {
                            typeof (GZipMessageInterceptor), //gzip compression
                            typeof (TripleDesMessageInterceptor) //encryption
                        }).Register(() => new TripleDesMessageInterceptorConfiguration(Convert.FromBase64String("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                Convert.FromBase64String("aaaaaaaaaaa=")), LifeStyles.Singleton),
                    heartBeatTime, heartBeatMonitorTime, route);
            }
            else
            {
                queue.RunConsumer(queueName, connectionString, false, logProvider, runTime, messageCount, workerCount, timeOut,
                    serviceRegister => serviceRegister.Register<IRollbackMessage, MessageProcessingFailRollBack>(LifeStyles.Singleton),
                    heartBeatTime, heartBeatMonitorTime, route);
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
