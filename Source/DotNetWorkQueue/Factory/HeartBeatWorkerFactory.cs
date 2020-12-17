// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
    /// <inheritdoc />
    public class HeartBeatWorkerFactory : IHeartBeatWorkerFactory
    {
        private readonly IHeartBeatConfiguration _configuration;
        private readonly ISendHeartBeat _sendHeartBeat;
        private readonly IHeartBeatScheduler _scheduler;
        private readonly ILogger _logFactory;
        private readonly IWorkerHeartBeatNotificationFactory _heartBeatNotificationFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="HeartBeatWorkerFactory" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="sendHeartBeat">The send heart beat module.</param>
        /// <param name="scheduler">The scheduler.</param>
        /// <param name="logFactory">The log factory.</param>
        /// <param name="heartBeatNotificationFactory">The heart beat notification factory.</param>
        public HeartBeatWorkerFactory(IHeartBeatConfiguration configuration,
            ISendHeartBeat sendHeartBeat,
            IHeartBeatScheduler scheduler,
            ILogger logFactory,
            IWorkerHeartBeatNotificationFactory heartBeatNotificationFactory)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => sendHeartBeat, sendHeartBeat);
            Guard.NotNull(() => scheduler, scheduler);
            Guard.NotNull(() => logFactory, logFactory);
            Guard.NotNull(() => heartBeatNotificationFactory, heartBeatNotificationFactory);

            _configuration = configuration;
            _sendHeartBeat = sendHeartBeat;
            _scheduler = scheduler;
            _logFactory = logFactory;
            _heartBeatNotificationFactory = heartBeatNotificationFactory;
        }

        /// <inheritdoc />
        public IHeartBeatWorker Create(IMessageContext context)
        {
            IHeartBeatWorker hb;
            if (_configuration.Enabled)
            {
                hb = new HeartBeatWorker(_configuration, context, _sendHeartBeat, _scheduler, _logFactory,
                    _heartBeatNotificationFactory);
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
