// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using DotNetWorkQueue.Transport.Redis.Basic.Command;
namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Clears expired messages from the queue
    /// </summary>
    internal class RedisQueueClearExpiredMessages: IClearExpiredMessages
    {
        private readonly ICommandHandlerWithOutput<ClearExpiredMessagesCommand, long> _commandReset;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueClearExpiredMessages"/> class.
        /// </summary>
        /// <param name="commandReset">The command reset.</param>
        public RedisQueueClearExpiredMessages(ICommandHandlerWithOutput<ClearExpiredMessagesCommand, long> commandReset)
        {
            _commandReset = commandReset;
        }

        /// <summary>
        /// Clears the messages.
        /// </summary>
        /// <param name="cancelToken">The cancel token.</param>
        public long ClearMessages(System.Threading.CancellationToken cancelToken)
        {
            var counter = _commandReset.Handle(new ClearExpiredMessagesCommand());
            var total = counter;
            while (counter > 0)
            {
                if (cancelToken.IsCancellationRequested)
                    return total;
                counter = _commandReset.Handle(new ClearExpiredMessagesCommand());
                total += counter;
            }
            return total;
        }
    }
}
