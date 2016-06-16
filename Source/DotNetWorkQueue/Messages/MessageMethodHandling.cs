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
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Logging;
namespace DotNetWorkQueue.Messages
{
    /// <summary>
    /// Handles processing of linq expression tree messages.
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IMessageMethodHandling" />
    public class MessageMethodHandling: IMessageMethodHandling
    {
        private readonly IExpressionSerializer _serializer;
        private readonly ILog _log;
        private readonly IQueueContainer _queueContainer;
        private readonly ILinqCompiler _linqCompiler;
        private readonly ICompositeSerialization _compositeSerialization;

        private readonly Dictionary<IConnectionInformation, IProducerQueueRpc<object>> _rpcQueues;
        private readonly object _rpcLock = new object();
        private int _disposeCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageMethodHandling" /> class.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="queueContainer">The queue container.</param>
        /// <param name="log">The log.</param>
        /// <param name="linqCompiler">The method compiler.</param>
        /// <param name="compositeSerialization">The composite serialization.</param>
        public MessageMethodHandling(IExpressionSerializer serializer,
            IQueueContainer queueContainer,
            ILogFactory log, 
            ILinqCompiler linqCompiler, 
            ICompositeSerialization compositeSerialization)
        {
            Guard.NotNull(() => serializer, serializer);
            Guard.NotNull(() => queueContainer, queueContainer);
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => linqCompiler, linqCompiler);
            Guard.NotNull(() => compositeSerialization, compositeSerialization);

            _serializer = serializer;
            _queueContainer = queueContainer;
            _linqCompiler = linqCompiler;
            _compositeSerialization = compositeSerialization;
            _log = log.Create();

            _rpcQueues = new Dictionary<IConnectionInformation, IProducerQueueRpc<object>>();
        }

        /// <summary>
        /// Handles processing of linq expression tree messages.
        /// </summary>
        /// <param name="receivedMessage">The received message.</param>
        /// <param name="workerNotification">The worker notification.</param>
        public void HandleExecution(IReceivedMessage<MessageExpression> receivedMessage, IWorkerNotification workerNotification)
        {
            ThrowIfDisposed();
            Guard.NotNull(() => receivedMessage, receivedMessage);
            Guard.NotNull(() => workerNotification, workerNotification);

            switch (receivedMessage.Body.PayLoad)
            {
                case MessageExpressionPayloads.Action:
                    HandleAction(receivedMessage, workerNotification);
                    break;
                case MessageExpressionPayloads.Function:
                    HandleFunction(receivedMessage, workerNotification);
                    break;
                case MessageExpressionPayloads.ActionText:
                    var targetMethod =
                        _linqCompiler.CompileAction(
                            _compositeSerialization.InternalSerializer.ConvertBytesTo<LinqExpressionToRun>(
                                receivedMessage.Body.SerializedExpression));
                    HandleAction(targetMethod, receivedMessage, workerNotification);

                    break;
                case MessageExpressionPayloads.FunctionText:
                    var targetFunction =
                        _linqCompiler.CompileFunction(
                            _compositeSerialization.InternalSerializer.ConvertBytesTo<LinqExpressionToRun>(
                                receivedMessage.Body.SerializedExpression));
                    HandleFunction(targetFunction, receivedMessage, workerNotification);
                    break;
                default:
                    throw new DotNetWorkQueueException($"The method type of {receivedMessage.Body.PayLoad} is not implemented");
            }
        }

        /// <summary>
        /// De-serializes and runs a compiled linq expression.
        /// </summary>
        /// <param name="receivedMessage">The received message.</param>
        /// <param name="workerNotification">The worker notification.</param>
        private void HandleAction(IReceivedMessage<MessageExpression> receivedMessage, IWorkerNotification workerNotification)
        {
            var target = _serializer.ConvertBytesToMethod(receivedMessage.Body.SerializedExpression);
            try
            {
                HandleAction(target.Compile(), receivedMessage, workerNotification);
            }
            catch (Exception error) //throw the real exception if needed
            {
                if (error.Message == "Exception has been thrown by the target of an invocation." &&
                    error.InnerException != null)
                {
                    throw error.InnerException;
                }
                throw;
            }
        }

        /// <summary>
        /// Runs a compiled linq expression.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="receivedMessage">The received message.</param>
        /// <param name="workerNotification">The worker notification.</param>
        private void HandleAction(Action<IReceivedMessage<MessageExpression>, IWorkerNotification> action, IReceivedMessage<MessageExpression> receivedMessage, IWorkerNotification workerNotification)
        {
            action.DynamicInvoke(receivedMessage, workerNotification);
        }

        /// <summary>
        /// De-serializes and runs a compiled linq func expression.
        /// </summary>
        /// <param name="receivedMessage">The received message.</param>
        /// <param name="workerNotification">The worker notification.</param>
        private void HandleFunction(IReceivedMessage<MessageExpression> receivedMessage,
            IWorkerNotification workerNotification)
        {
            var target = _serializer.ConvertBytesToFunction(receivedMessage.Body.SerializedExpression);
            try
            {
                HandleFunction(target.Compile(), receivedMessage, workerNotification);
            }
            catch (Exception error) //throw the real exception if needed
            {
                if (error.Message == "Exception has been thrown by the target of an invocation." &&
                    error.InnerException != null)
                {
                    throw error.InnerException;
                }
                throw;
            }
        }

        /// <summary>
        /// De-serializes and runs a compiled linq func expression.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <param name="receivedMessage">The received message.</param>
        /// <param name="workerNotification">The worker notification.</param>
        private void HandleFunction(Func<IReceivedMessage<MessageExpression>, IWorkerNotification, object> function, IReceivedMessage<MessageExpression> receivedMessage, IWorkerNotification workerNotification)
        {
            var result = function.DynamicInvoke(receivedMessage, workerNotification);
            if (result == null) return;

            //if we have a connection, this is an rpc request
            var connection =
                receivedMessage.GetHeader(workerNotification.HeaderNames.StandardHeaders.RpcConnectionInfo);

            //if no connection, then this was not RPC
            if (connection == null) return;

            var timeOut =
                receivedMessage.GetHeader(workerNotification.HeaderNames.StandardHeaders.RpcTimeout).Timeout;

            //if we don't have an RPC queue for this queue, create one
            CreateRpcModuleIfNeeded(connection);

            //send the response
            var response =
                _rpcQueues[connection].Send(
                    result,
                    _rpcQueues[connection].CreateResponse(receivedMessage.MessageId, timeOut));

            if (response.HasError)
            {
                _log.ErrorException("Failed to send a response for message {0}", response.SendingException, receivedMessage.MessageId.Id.Value);
            }
        }


        /// <summary>
        /// Creates an RPC module for sending responses if one does not already exist.
        /// </summary>
        /// <remarks>The connection is used as the key</remarks>
        /// <param name="connection">The connection.</param>
        private void CreateRpcModuleIfNeeded(IConnectionInformation connection)
        {
            lock (_rpcLock)
            {
                if (!_rpcQueues.ContainsKey(connection))
                {
                    _rpcQueues.Add(connection,
                        _queueContainer.CreateProducerRpc<object>(connection.QueueName,
                            connection.ConnectionString));
                }
            }
        }

        #region IDispose, IIsDisposed
        /// <summary>
        /// Throws an exception if this instance has been disposed.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <exception cref="System.ObjectDisposedException"></exception>
        protected void ThrowIfDisposed([CallerMemberName] string name = "")
        {
            if (Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0)
            {
                throw new ObjectDisposedException(name);
            }
        }
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (Interlocked.Increment(ref _disposeCount) != 1) return;

            foreach (var queue in _rpcQueues.Values)
            {
                queue.Dispose();
            }
            _rpcQueues.Clear();
        }

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed => Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0;

        #endregion
    }
}
