// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using System.Linq.Expressions;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Allows executing remote calls as a linq expression and returning the result of the expression.
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IRpcMethodQueue" />
    public class RpcMethodQueue : IRpcMethodQueue
    {
        private readonly IRpcQueue<object, MessageExpression> _queue;
        private readonly IExpressionSerializer _serializer;
        private readonly ICompositeSerialization _compositeSerialization;

        /// <summary>
        /// Initializes a new instance of the <see cref="RpcMethodQueue" /> class.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="compositeSerialization">The composite serialization.</param>
        public RpcMethodQueue(IRpcQueue<object, MessageExpression> queue,
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
        /// <summary>
        /// The queue configuration
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public QueueRpcConfiguration Configuration => _queue.Configuration;

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed => _queue.IsDisposed;

        /// <summary>
        /// Gets a value indicating whether this <see cref="IRpcQueue{TReceivedMessage, TSendMessage}" /> is started.
        /// </summary>
        /// <value>
        ///   <c>true</c> if started; otherwise, <c>false</c>.
        /// </value>
        public bool Started => _queue.Started;

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

        /// <summary>
        /// Sends the specified linqExpression for execution.
        /// </summary>
        /// <param name="method">The linqExpression.</param>
        /// <param name="timeOut">The time out.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        /// <remarks>
        /// Your expression must return a type of object, or the JSON serializer may throw casting errors
        /// </remarks>
        public IReceivedMessage<object> Send(Expression<Func<IReceivedMessage<MessageExpression>, IWorkerNotification, object>> method, TimeSpan timeOut, IAdditionalMessageData data = null)
        {
            var message = new MessageExpression(MessageExpressionPayloads.Function, _serializer.ConvertFunctionToBytes(method));
            return _queue.Send(message, timeOut, data);
        }

        /// <summary>
        /// Sends the specified linqExpression for execution.
        /// </summary>
        /// <param name="method">The linqExpression.</param>
        /// <param name="timeOut">The time out.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        /// <remarks>
        /// Your expression must return a type of object, or the JSON serializer may throw casting errors
        /// </remarks>
        public async Task<IReceivedMessage<object>> SendAsync(Expression<Func<IReceivedMessage<MessageExpression>, IWorkerNotification, object>> method, TimeSpan timeOut, IAdditionalMessageData data = null)
        {
            var message = new MessageExpression(MessageExpressionPayloads.Function, _serializer.ConvertFunctionToBytes(method));
            return await _queue.SendAsync(message, timeOut, data).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends the specified linqExpression for execution.
        /// </summary>
        /// <param name="linqExpression">The linqExpression.</param>
        /// <param name="timeOut">The time out.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        /// <remarks>Your expression must return a type of object, or the JSON serializer may throw casting errors</remarks>
        public IReceivedMessage<object> Send(LinqExpressionToRun linqExpression, TimeSpan timeOut, IAdditionalMessageData data = null)
        {
            var message = new MessageExpression(MessageExpressionPayloads.FunctionText, _compositeSerialization.InternalSerializer.ConvertToBytes(linqExpression));
            return _queue.Send(message, timeOut, data);
        }

        /// <summary>
        /// Sends the specified linqExpression for execution.
        /// </summary>
        /// <param name="linqExpression">The linqExpression.</param>
        /// <param name="timeOut">The time out.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        /// <remarks>Your expression must return a type of object, or the JSON serializer may throw casting errors</remarks>
        public async Task<IReceivedMessage<object>> SendAsync(LinqExpressionToRun linqExpression, TimeSpan timeOut,
            IAdditionalMessageData data = null)
        {
            var message = new MessageExpression(MessageExpressionPayloads.FunctionText, _compositeSerialization.InternalSerializer.ConvertToBytes(linqExpression));
            return await _queue.SendAsync(message, timeOut, data).ConfigureAwait(false);
        }

        /// <summary>
        /// Starts the queue.
        /// </summary>
        /// <remarks>
        /// This must be called after setting any configuration options, and before sending any messages.
        /// </remarks>
        public void Start()
        {
            _queue.Start();
        }
    }
}
