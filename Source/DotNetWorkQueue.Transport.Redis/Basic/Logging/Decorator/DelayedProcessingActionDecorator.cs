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
using System.Threading;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Transport.Redis.Basic.Logging.Decorator
{
    /// <inheritdoc />
    internal class DelayedProcessingActionDecorator : IDelayedProcessingAction
    {
        private readonly IDelayedProcessingAction _handler;
        private readonly ILogger _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessageQueryDecorator" /> class.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="handler">The handler.</param>
        public DelayedProcessingActionDecorator(ILogger log,
            IDelayedProcessingAction handler)
        {
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => handler, handler);

            _log = log;
            _handler = handler;
        }

        /// <inheritdoc />
        public long Run(CancellationToken token)
        {
            var records = _handler.Run(token);
            if (records > 0)
            {
                _log.LogInformation($"Moved {records} records from the delayed queue to the pending queue");
            }
            return records;
        }
    }
}
