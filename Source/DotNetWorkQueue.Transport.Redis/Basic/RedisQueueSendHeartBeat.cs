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
using System.Threading.Tasks;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Sends a heartbeat for a worker
    /// </summary>
    internal class RedisQueueSendHeartBeat : ISendHeartBeat
    {
        private readonly ICommandHandlerWithOutput<SendHeartBeatCommand<string>, long> _sendHeartBeat;
        private readonly ICommandHandlerWithOutputAsync<SendHeartBeatCommand<string>, long> _sendHeartBeatAsync;
        private readonly IUnixTimeFactory _unixTimeFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueSendHeartBeat"/> class.
        /// </summary>
        /// <param name="sendHeartBeat">The send heart beat.</param>
        /// <param name="sendHeartBeatAsync">The send heart beat, asynchronous.</param>
        /// <param name="unixTimeFactory">The unix time factory.</param>
        public RedisQueueSendHeartBeat(ICommandHandlerWithOutput<SendHeartBeatCommand<string>, long> sendHeartBeat,
            ICommandHandlerWithOutputAsync<SendHeartBeatCommand<string>, long> sendHeartBeatAsync,
            IUnixTimeFactory unixTimeFactory)
        {
            Guard.NotNull(sendHeartBeat);
            Guard.NotNull(sendHeartBeatAsync);
            Guard.NotNull(unixTimeFactory);

            _sendHeartBeat = sendHeartBeat;
            _sendHeartBeatAsync = sendHeartBeatAsync;
            _unixTimeFactory = unixTimeFactory;
        }

        /// <summary>
        /// Updates the heart beat for a record.
        /// </summary>
        /// <param name="context">The context.</param>
        public IHeartBeatStatus Send(IMessageContext context)
        {
            if (context.MessageId == null || !context.MessageId.HasValue) return null;
            var unixTime = _sendHeartBeat.Handle(new SendHeartBeatCommand<string>(context.MessageId.Id.Value.ToString()));
            return Status(context, unixTime);
        }

        /// <summary>
        /// Updates the heart beat for a record.
        /// </summary>
        /// <param name="context">The context.</param>
        public async Task<IHeartBeatStatus> SendAsync(IMessageContext context)
        {
            if (context.MessageId == null || !context.MessageId.HasValue) return null;
            var unixTime = await _sendHeartBeatAsync
                .HandleAsync(new SendHeartBeatCommand<string>(context.MessageId.Id.Value.ToString()))
                .ConfigureAwait(false);
            return Status(context, unixTime);
        }

        private IHeartBeatStatus Status(IMessageContext context, long unixTime)
        {
            //zero means the message was no longer in the working set, so nothing was renewed - report it
            //as no heartbeat rather than as one dated at the epoch
            if (unixTime <= 0)
                return new HeartBeatStatus(context.MessageId, null);

            return new HeartBeatStatus(context.MessageId, _unixTimeFactory.Create().DateTimeFromUnixTimestampMilliseconds(unixTime)); //UTC
        }
    }
}
