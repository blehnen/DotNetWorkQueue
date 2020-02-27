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
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Sends a heartbeat for a worker
    /// </summary>
    internal class RedisQueueSendHeartBeat: ISendHeartBeat
    {
        private readonly ICommandHandlerWithOutput<SendHeartBeatCommand, long> _sendHeartBeat;
        private readonly IUnixTimeFactory _unixTimeFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueSendHeartBeat"/> class.
        /// </summary>
        /// <param name="sendHeartBeat">The send heart beat.</param>
        /// <param name="unixTimeFactory">The unix time factory.</param>
        public RedisQueueSendHeartBeat(ICommandHandlerWithOutput<SendHeartBeatCommand, long> sendHeartBeat, 
            IUnixTimeFactory unixTimeFactory)
        {
            Guard.NotNull(() => sendHeartBeat, sendHeartBeat);
            Guard.NotNull(() => unixTimeFactory, unixTimeFactory);

            _sendHeartBeat = sendHeartBeat;
            _unixTimeFactory = unixTimeFactory;
        }

        /// <summary>
        /// Updates the heart beat for a record.
        /// </summary>
        /// <param name="context">The context.</param>
        public IHeartBeatStatus Send(IMessageContext context)
        {
            if (context.MessageId == null || !context.MessageId.HasValue) return null;
            var unixTime = _sendHeartBeat.Handle(new SendHeartBeatCommand((RedisQueueId)context.MessageId));
            return new HeartBeatStatus(context.MessageId, _unixTimeFactory.Create().DateTimeFromUnixTimestampMilliseconds(unixTime)); //UTC 
        }
    }
}
