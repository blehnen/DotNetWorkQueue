// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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

using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue.Metrics.Decorator
{
    /// <summary>
    /// Metrics for executing linq methods
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IMessageMethodHandling" />
    internal class MessageMethodHandlingDecorator: IMessageMethodHandling
    {
        private readonly IMessageMethodHandling _handler;
        private readonly ITimer _runMethodCompiledCodeTimer;
        private readonly ITimer _runFunctionCompiledCodeTimer;
        private readonly ITimer _runMethodDynamicCodeTimer;
        private readonly ITimer _runFunctionDynamicCodeTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageMethodHandlingDecorator" /> class.
        /// </summary>
        /// <param name="metrics">The metrics factory.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public MessageMethodHandlingDecorator(IMetrics metrics,
            IMessageMethodHandling handler,
            IConnectionInformation connectionInformation)
        {
            var name = handler.GetType().Name;
            _runMethodCompiledCodeTimer = metrics.Timer($"{connectionInformation.QueueName}.{name}.HandleCompiledMethodTimer", Units.Calls);
            _runFunctionCompiledCodeTimer = metrics.Timer($"{connectionInformation.QueueName}.{name}.HandleCompiledFunctionTimer", Units.Calls);
            _runMethodDynamicCodeTimer = metrics.Timer($"{connectionInformation.QueueName}.{name}.HandleDynamicMethodTimer", Units.Calls);
            _runFunctionDynamicCodeTimer = metrics.Timer($"{connectionInformation.QueueName}.{name}.HandleDynamicFunctionTimer", Units.Calls);
            _handler = handler;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _handler.Dispose();
        }

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed => _handler.IsDisposed;

        public void HandleExecution(IReceivedMessage<MessageExpression> receivedMessage, IWorkerNotification workerNotification)
        {
            switch (receivedMessage.Body.PayLoad)
            {
                case MessageExpressionPayloads.Action:
                case MessageExpressionPayloads.ActionRaw:
                    using (_runMethodCompiledCodeTimer.NewContext())
                    {
                        _handler.HandleExecution(receivedMessage, workerNotification);
                    }
                    break;
                case MessageExpressionPayloads.ActionText:
                    using (_runMethodDynamicCodeTimer.NewContext())
                    {
                        _handler.HandleExecution(receivedMessage, workerNotification);
                    }
                    break;
                case MessageExpressionPayloads.Function:
                    using (_runFunctionCompiledCodeTimer.NewContext())
                    {
                        _handler.HandleExecution(receivedMessage, workerNotification);
                    }
                    break;
                case MessageExpressionPayloads.FunctionText:
                    using (_runFunctionDynamicCodeTimer.NewContext())
                    {
                        _handler.HandleExecution(receivedMessage, workerNotification);
                    }
                    break;
                default:
                    throw new DotNetWorkQueueException($"Logic error - failed to handle type {receivedMessage.Body.PayLoad}");
            }
        }
    }
}
