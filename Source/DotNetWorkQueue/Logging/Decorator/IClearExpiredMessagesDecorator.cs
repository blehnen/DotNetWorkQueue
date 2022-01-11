﻿// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using System.Threading;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Logging.Decorator
{
    internal class ClearExpiredMessagesDecorator: IClearExpiredMessages
    {
        private readonly ILogger _log;
        private readonly IClearExpiredMessages _handler;
        private readonly IConnectionInformation _connectionInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClearExpiredMessagesDecorator" /> class.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="connectionInfo">The connection information.</param>
        public ClearExpiredMessagesDecorator(ILogger log,
            IClearExpiredMessages handler, 
            IConnectionInformation connectionInfo)
        {
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => handler, handler);
            Guard.NotNull(() => connectionInfo, connectionInfo);

            _log = log;
            _handler = handler;
            _connectionInfo = connectionInfo;
        }

        /// <summary>
        /// Clears the expired messages from the queue
        /// </summary>
        /// <param name="cancelToken">The cancel token. When signaled, processing should stop as soon as possible</param>
        /// <returns></returns>
        public long ClearMessages(CancellationToken cancelToken)
        {
            var count = _handler.ClearMessages(cancelToken);
            if (count > 0)
            {
                _log.LogInformation($"Deleted {count} expired messages from {_connectionInfo.QueueName}");
            }
            return count;
        }
    }
}
