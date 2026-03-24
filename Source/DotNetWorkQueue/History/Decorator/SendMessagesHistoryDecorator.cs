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
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetWorkQueue.Messages;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.History.Decorator
{
    internal class SendMessagesHistoryDecorator : ISendMessages
    {
        private readonly ISendMessages _handler;
        private readonly IWriteMessageHistory _history;
        private readonly IBaseTransportOptions _options;
        private readonly ILogger _log;

        public SendMessagesHistoryDecorator(ISendMessages handler,
            IWriteMessageHistory history,
            IBaseTransportOptions options,
            ILogger log)
        {
            _handler = handler;
            _history = history;
            _options = options;
            _log = log;
        }

        public IQueueOutputMessage Send(IMessage messageToSend, IAdditionalMessageData data)
        {
            var result = _handler.Send(messageToSend, data);
            RecordEnqueue(result, data);
            return result;
        }

        public IQueueOutputMessages Send(List<QueueMessage<IMessage, IAdditionalMessageData>> messages)
        {
            var result = _handler.Send(messages);
            if (_options.EnableHistory && _options.HistoryOptions.TrackEnqueue)
            {
                foreach (var msg in result)
                {
                    RecordEnqueue(msg, null);
                }
            }
            return result;
        }

        public async Task<IQueueOutputMessage> SendAsync(IMessage messageToSend, IAdditionalMessageData data)
        {
            var result = await _handler.SendAsync(messageToSend, data).ConfigureAwait(false);
            RecordEnqueue(result, data);
            return result;
        }

        public async Task<IQueueOutputMessages> SendAsync(List<QueueMessage<IMessage, IAdditionalMessageData>> messages)
        {
            var result = await _handler.SendAsync(messages).ConfigureAwait(false);
            if (_options.EnableHistory && _options.HistoryOptions.TrackEnqueue)
            {
                foreach (var msg in result)
                {
                    RecordEnqueue(msg, null);
                }
            }
            return result;
        }

        private void RecordEnqueue(IQueueOutputMessage result, IAdditionalMessageData data)
        {
            if (!_options.EnableHistory || !_options.HistoryOptions.TrackEnqueue) return;
            if (result.HasError || result.SentMessage?.MessageId == null || !result.SentMessage.MessageId.HasValue) return;

            try
            {
                var queueId = result.SentMessage.MessageId.Id.Value.ToString();
                var correlationId = result.SentMessage.CorrelationId?.Id?.Value?.ToString();
                var route = data?.Route;

                _history.RecordEnqueue(queueId, correlationId, route, null, null, null);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to record history for enqueue of message {MessageId}",
                    result.SentMessage.MessageId.Id.Value);
            }
        }
    }
}
