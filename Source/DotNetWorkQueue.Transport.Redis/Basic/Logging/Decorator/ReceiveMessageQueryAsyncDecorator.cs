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
using DotNetWorkQueue.Transport.Redis.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using System.Threading.Tasks;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Transport.Redis.Basic.Logging.Decorator
{
    /// <inheritdoc />
    internal class ReceiveMessageQueryAsyncDecorator : IQueryHandlerAsync<ReceiveMessageQuery, RedisMessage>
    {
        private readonly IQueryHandlerAsync<ReceiveMessageQuery, RedisMessage> _handler;
        private readonly ILogger _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessageQueryAsyncDecorator" /> class.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="handler">The handler.</param>
        public ReceiveMessageQueryAsyncDecorator(ILogger log,
            IQueryHandlerAsync<ReceiveMessageQuery, RedisMessage> handler)
        {
            Guard.NotNull(log);
            Guard.NotNull(handler);

            _log = log;
            _handler = handler;
        }

        /// <inheritdoc />
        public async Task<RedisMessage> HandleAsync(ReceiveMessageQuery query)
        {
            var result = await _handler.HandleAsync(query).ConfigureAwait(false);
            if (result != null && result.Expired && _log.IsEnabled(LogLevel.Debug))
                _log.LogDebug("Message {MessageId} expired before it could be processed", result.MessageId);
            return result;
        }
    }
}
