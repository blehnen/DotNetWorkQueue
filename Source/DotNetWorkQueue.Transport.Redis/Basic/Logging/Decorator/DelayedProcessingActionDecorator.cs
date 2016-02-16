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
using System.Threading;
using DotNetWorkQueue.Logging;
namespace DotNetWorkQueue.Transport.Redis.Basic.Logging.Decorator
{
    /// <summary>
    /// Logs how many records where moved
    /// </summary>
    internal class DelayedProcessingActionDecorator: IDelayedProcessingAction
    {
        private readonly IDelayedProcessingAction _handler;
        private readonly ILog _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessageQueryDecorator" /> class.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="handler">The handler.</param>
        public DelayedProcessingActionDecorator(ILogFactory log,
            IDelayedProcessingAction handler)
        {
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => handler, handler);

            _log = log.Create();
            _handler = handler;
        }

        /// <summary>
        /// Runs the action
        /// </summary>
        /// <param name="token">The cancel token.</param>
        /// <returns></returns>
        public long Run(CancellationToken token)
        {
            var records = _handler.Run(token);
            if (records > 0)
            {
                _log.InfoFormat("Moved {0} records from the delayed queue to the pending queue", records);
            }
            return records;
        }
    }
}
