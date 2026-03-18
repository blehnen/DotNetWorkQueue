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
using System;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.History.Decorator
{
    internal class ReceiveMessagesErrorHistoryDecorator : IReceiveMessagesError
    {
        private readonly IReceiveMessagesError _handler;
        private readonly IWriteMessageHistory _history;
        private readonly IHistoryConfiguration _config;
        private readonly ILogger _log;

        public ReceiveMessagesErrorHistoryDecorator(IReceiveMessagesError handler,
            IWriteMessageHistory history,
            IHistoryConfiguration config,
            ILogger log)
        {
            _handler = handler;
            _history = history;
            _config = config;
            _log = log;
        }

        public ReceiveMessagesErrorResult MessageFailedProcessing(IReceivedMessageInternal message, IMessageContext context, Exception exception)
        {
            var result = _handler.MessageFailedProcessing(message, context, exception);
            if (_config.Enabled && _config.TrackError && context.MessageId != null && context.MessageId.HasValue)
            {
                try
                {
                    var exceptionText = exception?.ToString();
                    if (exceptionText != null && exceptionText.Length > _config.MaxExceptionLength)
                        exceptionText = exceptionText.Substring(0, _config.MaxExceptionLength);

                    _history.RecordError(context.MessageId.Id.Value.ToString(), exceptionText);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Failed to record history for error of message {MessageId}", context.MessageId.Id.Value);
                }
            }
            return result;
        }
    }
}
