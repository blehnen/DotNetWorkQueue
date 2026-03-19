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
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Decorator that registers a per-message cancellation token before the handler runs
    /// and unregisters it after the handler completes (success, error, or exception).
    /// </summary>
    internal class MessageHandlerCancellationDecorator : IMessageHandler
    {
        private readonly IMessageHandler _handler;
        private readonly MessageCancellationTracker _tracker;
        private readonly ILogger _log;

        public MessageHandlerCancellationDecorator(IMessageHandler handler,
            MessageCancellationTracker tracker,
            ILogger log)
        {
            _handler = handler;
            _tracker = tracker;
            _log = log;
        }

        public void Handle(IReceivedMessageInternal message, IWorkerNotification workerNotification)
        {
            var queueId = GetQueueId(message);
            if (queueId != null)
            {
                try
                {
                    var workerTokens = workerNotification.WorkerStopping?.Tokens;
                    var linkedToken = workerTokens != null && workerTokens.Count > 0
                        ? _tracker.Register(queueId, workerTokens.ToArray())
                        : _tracker.Register(queueId);

                    workerNotification.MessageCancellation = new MessageCancellation(linkedToken);

                    _handler.Handle(message, workerNotification);
                }
                finally
                {
                    _tracker.Unregister(queueId);
                    workerNotification.MessageCancellation = null;
                }
            }
            else
            {
                _handler.Handle(message, workerNotification);
            }
        }

        private static string GetQueueId(IReceivedMessageInternal message)
        {
            if (message?.MessageId != null && message.MessageId.HasValue)
                return message.MessageId.Id.Value.ToString();
            return null;
        }
    }

    /// <summary>
    /// Async version of <see cref="MessageHandlerCancellationDecorator"/>.
    /// </summary>
    internal class MessageHandlerAsyncCancellationDecorator : IMessageHandlerAsync
    {
        private readonly IMessageHandlerAsync _handler;
        private readonly MessageCancellationTracker _tracker;
        private readonly ILogger _log;

        public MessageHandlerAsyncCancellationDecorator(IMessageHandlerAsync handler,
            MessageCancellationTracker tracker,
            ILogger log)
        {
            _handler = handler;
            _tracker = tracker;
            _log = log;
        }

        public async Task HandleAsync(IReceivedMessageInternal message, IWorkerNotification workerNotification)
        {
            var queueId = GetQueueId(message);
            if (queueId != null)
            {
                try
                {
                    var workerTokens = workerNotification.WorkerStopping?.Tokens;
                    var linkedToken = workerTokens != null && workerTokens.Count > 0
                        ? _tracker.Register(queueId, workerTokens.ToArray())
                        : _tracker.Register(queueId);

                    workerNotification.MessageCancellation = new MessageCancellation(linkedToken);

                    await _handler.HandleAsync(message, workerNotification).ConfigureAwait(false);
                }
                finally
                {
                    _tracker.Unregister(queueId);
                    workerNotification.MessageCancellation = null;
                }
            }
            else
            {
                await _handler.HandleAsync(message, workerNotification).ConfigureAwait(false);
            }
        }

        private static string GetQueueId(IReceivedMessageInternal message)
        {
            if (message?.MessageId != null && message.MessageId.HasValue)
                return message.MessageId.Id.Value.ToString();
            return null;
        }
    }
}
