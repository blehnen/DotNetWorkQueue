// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Factory
{
    /// <inheritdoc />
    public class HeartBeatWorkerFactory : IHeartBeatWorkerFactory
    {
        private readonly IHeartBeatConfiguration _configuration;
        private readonly ISendHeartBeat _sendHeartBeat;
        private readonly IHeartBeatGate _gate;
        private readonly ILogger _log;
        private readonly IWorkerHeartBeatNotificationFactory _heartBeatNotificationFactory;
        private readonly IGetTimeFactory _getTimeFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="HeartBeatWorkerFactory" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="sendHeartBeat">The send heart beat module.</param>
        /// <param name="gate">Bounds how many beats the consumer has in flight at once.</param>
        /// <param name="logFactory">The log factory.</param>
        /// <param name="heartBeatNotificationFactory">The heart beat notification factory.</param>
        /// <param name="getTimeFactory">The time factory; the transport's clock.</param>
        public HeartBeatWorkerFactory(IHeartBeatConfiguration configuration,
            ISendHeartBeat sendHeartBeat,
            IHeartBeatGate gate,
            ILogger logFactory,
            IWorkerHeartBeatNotificationFactory heartBeatNotificationFactory,
            IGetTimeFactory getTimeFactory)
        {
            Guard.NotNull(getTimeFactory);
            Guard.NotNull(configuration);
            Guard.NotNull(sendHeartBeat);
            Guard.NotNull(gate);
            Guard.NotNull(logFactory);
            Guard.NotNull(heartBeatNotificationFactory);

            _configuration = configuration;
            _sendHeartBeat = sendHeartBeat;
            _gate = gate;
            _log = logFactory;
            _heartBeatNotificationFactory = heartBeatNotificationFactory;
            _getTimeFactory = getTimeFactory;
        }

        /// <inheritdoc />
        public IHeartBeatWorker Create(IMessageContext context)
        {
            IHeartBeatWorker hb;
            if (_configuration.Enabled)
            {
                hb = new HeartBeatWorker(_configuration, context, _sendHeartBeat, _gate, _log,
                    _heartBeatNotificationFactory, _getTimeFactory);
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
