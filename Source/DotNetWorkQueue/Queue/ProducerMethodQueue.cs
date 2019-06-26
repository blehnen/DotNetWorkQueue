// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Queue
{
    /// <inheritdoc />
    public class ProducerMethodQueue : IProducerMethodQueue
    {
        private readonly IProducerQueue<MessageExpression> _queue;
        private readonly IExpressionSerializer _serializer;
        private readonly ICompositeSerialization _compositeSerialization;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProducerMethodQueue" /> class.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="compositeSerialization">The composite serialization.</param>
        public ProducerMethodQueue(IProducerQueue<MessageExpression> queue,
            IExpressionSerializer serializer, 
            ICompositeSerialization compositeSerialization)
        {
            Guard.NotNull(() => queue, queue);
            Guard.NotNull(() => serializer, serializer);
            Guard.NotNull(() => compositeSerialization, compositeSerialization);

            _queue = queue;
            _serializer = serializer;
            _compositeSerialization = compositeSerialization;
        }
        /// <inheritdoc />
        public QueueProducerConfiguration Configuration => _queue.Configuration;

        /// <inheritdoc />
        public bool IsDisposed => _queue.IsDisposed;

        /// <inheritdoc />
        public IQueueOutputMessages Send(List<QueueMessage<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>, IAdditionalMessageData>> methods, bool rawExpression = false)
        {
            var messages = new List<QueueMessage<MessageExpression, IAdditionalMessageData>>(methods.Count);
            foreach (var method in methods)
            {
                if (rawExpression)
                {
                    var message = new MessageExpression(MessageExpressionPayloads.ActionRaw, method.Message);
                    messages.Add(new QueueMessage<MessageExpression, IAdditionalMessageData>(message, method.MessageData));
                }
                else
                {
                    var message = new MessageExpression(MessageExpressionPayloads.Action, _serializer.ConvertMethodToBytes(method.Message));
                    messages.Add(new QueueMessage<MessageExpression, IAdditionalMessageData>(message, method.MessageData));
                }
            }
            return _queue.Send(messages);
        }

        /// <inheritdoc />
        public IQueueOutputMessages Send(List<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>> methods, bool rawExpression = false)
        {
            var messages = new List<MessageExpression>(methods.Count);
            messages.AddRange(rawExpression
                ? methods.Select(method => new MessageExpression(MessageExpressionPayloads.ActionRaw, method))
                : methods.Select(method =>
                    new MessageExpression(MessageExpressionPayloads.Action, _serializer.ConvertMethodToBytes(method))));
            return _queue.Send(messages);
        }

        /// <inheritdoc />
        public IQueueOutputMessage Send(Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> method, IAdditionalMessageData data = null, bool rawExpression = false)
        {
            if (rawExpression)
            {
                var message = new MessageExpression(MessageExpressionPayloads.ActionRaw,
                    method);
                return _queue.Send(message, data);
            }
            else
            {
                var message = new MessageExpression(MessageExpressionPayloads.Action,
                    _serializer.ConvertMethodToBytes(method));
                return _queue.Send(message, data);
            }
        }

        /// <inheritdoc />
        public async Task<IQueueOutputMessages> SendAsync(List<QueueMessage<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>, IAdditionalMessageData>> methods, bool rawExpression = false)
        {
            var messages = new List<QueueMessage<MessageExpression, IAdditionalMessageData>>(methods.Count);
            if (rawExpression)
            {
                messages.AddRange(from method in methods let message = new MessageExpression(MessageExpressionPayloads.ActionRaw, method.Message) select new QueueMessage<MessageExpression, IAdditionalMessageData>(message, method.MessageData));
            }
            else
            {
                messages.AddRange(from method in methods let message = new MessageExpression(MessageExpressionPayloads.Action, _serializer.ConvertMethodToBytes(method.Message)) select new QueueMessage<MessageExpression, IAdditionalMessageData>(message, method.MessageData));
            }
            return await _queue.SendAsync(messages).ConfigureAwait(false);
        }

#if NETFULL
        /// <inheritdoc />
        public IQueueOutputMessage Send(LinqExpressionToRun linqExpression, IAdditionalMessageData data = null)
        {
            var message = new MessageExpression(MessageExpressionPayloads.ActionText,
                _compositeSerialization.InternalSerializer.ConvertToBytes(linqExpression));
            return _queue.Send(message, data);
        }

        /// <inheritdoc />
        public IQueueOutputMessages Send(List<LinqExpressionToRun> methods)
        {
            var messages = new List<MessageExpression>(methods.Count);
            messages.AddRange(methods.Select(method => new MessageExpression(MessageExpressionPayloads.ActionText, _compositeSerialization.InternalSerializer.ConvertToBytes(method))));
            return _queue.Send(messages);
        }

        /// <inheritdoc />
        public IQueueOutputMessages Send(List<QueueMessage<LinqExpressionToRun, IAdditionalMessageData>> methods)
        {
            var messages = new List<QueueMessage<MessageExpression, IAdditionalMessageData>>(methods.Count);
            foreach (var method in methods)
            {
                var message = new MessageExpression(MessageExpressionPayloads.ActionText, _compositeSerialization.InternalSerializer.ConvertToBytes(method));
                messages.Add(new QueueMessage<MessageExpression, IAdditionalMessageData>(message, method.MessageData));
            }
            return _queue.Send(messages);
        }

        /// <inheritdoc />
        public async Task<IQueueOutputMessage> SendAsync(LinqExpressionToRun linqExpression, IAdditionalMessageData data = null)
        {
            var message = new MessageExpression(MessageExpressionPayloads.ActionText, _compositeSerialization.InternalSerializer.ConvertToBytes(linqExpression));
            return await _queue.SendAsync(message, data).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IQueueOutputMessages> SendAsync(List<LinqExpressionToRun> methods)
        {
            var messages = new List<MessageExpression>(methods.Count);
            messages.AddRange(methods.Select(method => new MessageExpression(MessageExpressionPayloads.ActionText, _compositeSerialization.InternalSerializer.ConvertToBytes(method))));
            return await _queue.SendAsync(messages).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IQueueOutputMessages> SendAsync(List<QueueMessage<LinqExpressionToRun, IAdditionalMessageData>> methods)
        {
            var messages = new List<QueueMessage<MessageExpression, IAdditionalMessageData>>(methods.Count);
            foreach (var method in methods)
            {
                var message = new MessageExpression(MessageExpressionPayloads.ActionText, _compositeSerialization.InternalSerializer.ConvertToBytes(method));
                messages.Add(new QueueMessage<MessageExpression, IAdditionalMessageData>(message, method.MessageData));
            }
            return await _queue.SendAsync(messages).ConfigureAwait(false);
        }
#endif

        /// <inheritdoc />
        public async Task<IQueueOutputMessages> SendAsync(List<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>> methods, bool rawExpression = false)
        {
            var messages = new List<MessageExpression>(methods.Count);
            messages.AddRange(rawExpression
                ? methods.Select(method => new MessageExpression(MessageExpressionPayloads.ActionRaw, method))
                : methods.Select(method =>
                    new MessageExpression(MessageExpressionPayloads.Action, _serializer.ConvertMethodToBytes(method))));
            return await _queue.SendAsync(messages).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IQueueOutputMessage> SendAsync(Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> method, IAdditionalMessageData data = null, bool rawExpression = false)
        {
            if (rawExpression)
            {
                var message = new MessageExpression(MessageExpressionPayloads.ActionRaw, method);
                return await _queue.SendAsync(message, data).ConfigureAwait(false);
            }
            else
            {
                var message = new MessageExpression(MessageExpressionPayloads.Action, _serializer.ConvertMethodToBytes(method));
                return await _queue.SendAsync(message, data).ConfigureAwait(false);
            }
        }

#region IDisposable Support
        private bool _disposedValue; // To detect redundant calls
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _queue.Dispose();
                }
                _disposedValue = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
#endregion
    }
}
