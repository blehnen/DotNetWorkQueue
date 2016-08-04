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
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Factory
{
    /// <summary>
    /// Creates new instances of <see cref="IHeartBeatWorker"/>
    /// </summary>
    public class HeartBeatWorkerFactory : IHeartBeatWorkerFactory
    {
        private readonly IHeartBeatConfiguration _configuration;
        private readonly ISendHeartBeat _sendHeartBeat;
        private readonly IHeartBeatThreadPoolFactory _threadPool;
        private readonly ILogFactory _logFactory;
        private readonly IWorkerHeartBeatNotificationFactory _heartBeatNotificationFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="HeartBeatWorkerFactory" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="sendHeartBeat">The send heart beat module.</param>
        /// <param name="threadPool">The thread pool.</param>
        /// <param name="logFactory">The log factory.</param>
        /// <param name="heartBeatNotificationFactory">The heart beat notification factory.</param>
        public HeartBeatWorkerFactory(IHeartBeatConfiguration configuration,
            ISendHeartBeat sendHeartBeat,
            IHeartBeatThreadPoolFactory threadPool,
            ILogFactory logFactory,
            IWorkerHeartBeatNotificationFactory heartBeatNotificationFactory)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => sendHeartBeat, sendHeartBeat);
            Guard.NotNull(() => threadPool, threadPool);
            Guard.NotNull(() => logFactory, logFactory);
            Guard.NotNull(() => heartBeatNotificationFactory, heartBeatNotificationFactory);

            _configuration = configuration;
            _sendHeartBeat = sendHeartBeat;
            _threadPool = threadPool;
            _logFactory = logFactory;
            _heartBeatNotificationFactory = heartBeatNotificationFactory;
        }

        /// <summary>
        /// Creates a new instance of a <see cref="IHeartBeatWorker" /> and starts it.
        /// </summary>
        /// <param name="context">The message context that should receive heartbeat updates.</param>
        /// <returns></returns>
        public IHeartBeatWorker Create(IMessageContext context)
        {
            IHeartBeatWorker hb;
            if (_configuration.Enabled)
            {
                hb = new HeartBeatWorker(_configuration, context, _sendHeartBeat, _threadPool.Create(), _logFactory, _heartBeatNotificationFactory);
            }
            else
            {
                hb = new HeartBeatWorkerNoOp(context);
            }
            hb.Start();
            return hb;
        }
    }
}
