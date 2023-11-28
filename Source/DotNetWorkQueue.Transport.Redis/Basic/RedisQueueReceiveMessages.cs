// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using DotNetWorkQueue.Transport.Redis.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;
using System;
using System.Linq;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// receives messages from the dequeue process
    /// </summary>
    internal class RedisQueueReceiveMessages : IReceiveMessages
    {
        private readonly IRedisQueueWorkSubFactory _workSubFactory;
        private readonly IQueryHandler<ReceiveMessageQuery, RedisMessage> _receiveMessage;
        private readonly ITransportHandleMessage _handleMessage;
        private readonly ICancelWork _cancelWork;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueReceiveMessages" /> class.
        /// </summary>
        /// <param name="workSubFactory">The work sub factory.</param>
        /// <param name="receiveMessage">The receive message.</param>
        /// <param name="handleMessage">The handle message.</param>
        /// <param name="cancelWork">The cancel work.</param>
        public RedisQueueReceiveMessages(IRedisQueueWorkSubFactory workSubFactory,
            IQueryHandler<ReceiveMessageQuery, RedisMessage> receiveMessage,
            ITransportHandleMessage handleMessage,
            IQueueCancelWork cancelWork)
        {
            Guard.NotNull(() => workSubFactory, workSubFactory);
            Guard.NotNull(() => receiveMessage, receiveMessage);
            Guard.NotNull(() => handleMessage, handleMessage);
            Guard.NotNull(() => cancelWork, cancelWork);

            _receiveMessage = receiveMessage;
            _handleMessage = handleMessage;
            _cancelWork = cancelWork;
            _workSubFactory = workSubFactory;
        }

        /// <summary>
        /// Receives a new message.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public IReceivedMessageInternal ReceiveMessage(IMessageContext context)
        {
            context.Commit += ContextOnCommit;
            context.Rollback += ContextOnRollback;
            context.Cleanup += Context_Cleanup;

            using (
                var workSub = _workSubFactory.Create())
            {
                while (true)
                {
                    if (_cancelWork.Tokens.Any(m => m.IsCancellationRequested))
                    {
                        return null;
                    }

                    var message = GetMessage(context);
                    if (message != null && !message.Expired)
                    {
                        return message.Message;
                    }

                    if (_cancelWork.Tokens.Any(m => m.IsCancellationRequested))
                    {
                        return null;
                    }

                    workSub.Reset();
                    message = GetMessage(context);
                    if (message != null && !message.Expired)
                    {
                        return message.Message;
                    }
                    if (message != null && message.Expired)
                    {
                        continue;
                    }
                    if (workSub.Wait())
                    {
                        continue;
                    }

                    return null;
                }
            }
        }

        /// <inheritdoc />
        public bool IsBlockingOperation => true; //we use signals to indicate new items, so yes

        /// <summary>
        /// Gets the next message from the queue
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        private RedisMessage GetMessage(IMessageContext context)
        {
            var message = _receiveMessage.Handle(new ReceiveMessageQuery(context));
            if (message == null) return null;
            if (!message.Expired)
            {
                context.SetMessageAndHeaders(message.Message.MessageId, message.Message.CorrelationId, message.Message.Headers);
            }
            return message;
        }
        /// <summary>
        /// On Rollback
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArgs">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void ContextOnRollback(object sender, EventArgs eventArgs)
        {
            _handleMessage.RollbackMessage.Rollback((IMessageContext)sender);
        }

        /// <summary>
        /// On Commit
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArgs">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void ContextOnCommit(object sender, EventArgs eventArgs)
        {
            _handleMessage.CommitMessage.Commit((IMessageContext)sender);
        }
        /// <summary>
        /// Handles the Cleanup event of the context control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void Context_Cleanup(object sender, EventArgs e)
        {
            var context = (IMessageContext)sender;
            ContextCleanup(context);
        }
        /// <summary>
        /// Clean up the message context when processing is done
        /// </summary>
        /// <param name="context">The context.</param>
        private void ContextCleanup(IMessageContext context)
        {
            context.Commit -= ContextOnCommit;
            context.Rollback -= ContextOnRollback;
            context.Cleanup -= Context_Cleanup;
        }
    }
}
