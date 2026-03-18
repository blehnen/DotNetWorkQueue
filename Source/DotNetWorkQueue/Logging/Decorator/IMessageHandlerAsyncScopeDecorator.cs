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
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Logging.Decorator
{
    /// <summary>
    /// Pushes the message ID into the ILogger scope during async handler execution.
    /// Any logging the user does inside their handler automatically carries the message ID.
    /// </summary>
    internal class MessageHandlerAsyncScopeDecorator : IMessageHandlerAsync
    {
        private readonly IMessageHandlerAsync _handler;
        private readonly ILogger _log;

        public MessageHandlerAsyncScopeDecorator(IMessageHandlerAsync handler, ILogger log)
        {
            _handler = handler;
            _log = log;
        }

        public async Task HandleAsync(IReceivedMessageInternal message, IWorkerNotification workerNotification)
        {
            if (message?.MessageId != null && message.MessageId.HasValue)
            {
                using (_log.BeginScope(new Dictionary<string, object>
                {
                    ["MessageId"] = message.MessageId.Id.Value.ToString(),
                    ["CorrelationId"] = message.CorrelationId?.Id?.Value?.ToString()
                }))
                {
                    await _handler.HandleAsync(message, workerNotification).ConfigureAwait(false);
                }
            }
            else
            {
                await _handler.HandleAsync(message, workerNotification).ConfigureAwait(false);
            }
        }
    }
}
