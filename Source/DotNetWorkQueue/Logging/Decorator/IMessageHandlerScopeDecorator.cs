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
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Logging.Decorator
{
    /// <summary>
    /// Pushes the message ID into the ILogger scope during handler execution.
    /// Any logging the user does inside their handler automatically carries the message ID.
    /// </summary>
    internal class MessageHandlerScopeDecorator : IMessageHandler
    {
        private readonly IMessageHandler _handler;
        private readonly ILogger _log;

        public MessageHandlerScopeDecorator(IMessageHandler handler, ILogger log)
        {
            _handler = handler;
            _log = log;
        }

        public void Handle(IReceivedMessageInternal message, IWorkerNotification workerNotification)
        {
            if (message?.MessageId != null && message.MessageId.HasValue)
            {
                using (_log.BeginScope(new Dictionary<string, object>
                {
                    ["MessageId"] = message.MessageId.Id.Value.ToString(),
                    ["CorrelationId"] = message.CorrelationId?.Id?.Value?.ToString()
                }))
                {
                    _handler.Handle(message, workerNotification);
                }
            }
            else
            {
                _handler.Handle(message, workerNotification);
            }
        }
    }
}
