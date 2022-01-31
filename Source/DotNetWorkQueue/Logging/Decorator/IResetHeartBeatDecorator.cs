// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using System.Collections.Generic;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Logging.Decorator
{
    internal class ResetHeartBeatDecorator : IResetHeartBeat
    {
        private readonly ILogger _log;
        private readonly IResetHeartBeat _handler;
        private readonly QueueConsumerConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResetHeartBeatDecorator" /> class.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="configuration">The configuration.</param>
        public ResetHeartBeatDecorator(ILogger log,
            IResetHeartBeat handler,
             QueueConsumerConfiguration configuration)
        {
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => handler, handler);
            Guard.NotNull(() => configuration, configuration);

            _log = log;
            _handler = handler;
            _configuration = configuration;
        }

        public List<ResetHeartBeatOutput> Reset(CancellationToken cancelToken)
        {
            var count = _handler.Reset(cancelToken);
            if (count.Count > 0)
            {
                _log.LogInformation(
                   $"Reset the status of {count.Count} records that where outside of the heartbeat window of {_configuration.HeartBeat.Time.TotalSeconds} seconds");
            }
            return count;
        }
    }
}
