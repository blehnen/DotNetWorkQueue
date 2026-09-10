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
using DotNetWorkQueue.Transport.Redis.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// receives messages from the dequeue process
    /// </summary>
    internal class RedisQueueReceiveMessages : IReceiveMessages
    {
        private readonly IRedisQueueWorkSubFactory _workSubFactory;
        private readonly IQueryHandler<ReceiveMessageQuery, RedisMessage> _receiveMessage;
        private readonly IQueryHandlerAsync<ReceiveMessageQuery, RedisMessage> _receiveMessageAsync;
        private readonly ITransportHandleMessage _handleMessage;
        private readonly ICancelWork _cancelWork;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueReceiveMessages" /> class.
        /// </summary>
        /// <param name="workSubFactory">The work sub factory.</param>
        /// <param name="receiveMessage">The receive message.</param>
        /// <param name="receiveMessageAsync">The receive message handler used by the async path.</param>
        /// <param name="handleMessage">The handle message.</param>
        /// <param name="cancelWork">The cancel work.</param>
        public RedisQueueReceiveMessages(IRedisQueueWorkSubFactory workSubFactory,
            IQueryHandler<ReceiveMessageQuery, RedisMessage> receiveMessage,
            IQueryHandlerAsync<ReceiveMessageQuery, RedisMessage> receiveMessageAsync,
            ITransportHandleMessage handleMessage,
            IQueueCancelWork cancelWork)
        {
            Guard.NotNull(workSubFactory);
            Guard.NotNull(receiveMessage);
            Guard.NotNull(receiveMessageAsync);
            Guard.NotNull(handleMessage);
            Guard.NotNull(cancelWork);

            _receiveMessage = receiveMessage;
            _receiveMessageAsync = receiveMessageAsync;
            _handleMessage = handleMessage;
            _cancelWork = cancelWork;
            _workSubFactory = workSubFactory;
        }
        //Cached so the wiring below does not build a delegate for each of these method groups on
        //every message: a subscribe and an unsubscribe each built their own, six per message. One
        //instance of this class serves one worker, and even shared the worst case is a duplicate
        //delegate that unsubscribes just as well, since removal compares target and method rather
        //than reference.
        private EventHandler _cachedCommit;
        private AsyncEventHandler _cachedCommitAsync;
        private EventHandler _cachedRollback;
        private EventHandler _cachedCleanup;


        /// <summary>
        /// Receives a new message.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public IReceivedMessageInternal ReceiveMessage(IMessageContext context)
        {
            context.Commit += _cachedCommit ??= ContextOnCommit;
            //Both are subscribed: the synchronous consumer raises Commit, the asynchronous one
            //raises CommitAsync. A context only ever raises one of them.
            context.CommitAsync += _cachedCommitAsync ??= ContextOnCommitAsync;
            context.Rollback += _cachedRollback ??= ContextOnRollback;
            context.Cleanup += _cachedCleanup ??= Context_Cleanup;

            using (
                var workSub = _workSubFactory.Create())
            {
                while (true)
                {
                    if (_cancelWork.AnyCancellationRequested())
                    {
                        return null;
                    }

                    var message = GetMessage(context);
                    if (message != null && !message.Expired)
                    {
                        return message.Message;
                    }

                    if (_cancelWork.AnyCancellationRequested())
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
        /// <remarks>
        /// <paramref name="cancellation"/> is honoured everywhere this method can wait: both loop
        /// checks test it alongside the queue's own tokens, and it is linked into the work-signal wait,
        /// which is where a de-queue spends its time when the queue is empty.
        ///
        /// What it cannot do is abort a Lua call already on the wire. SE.Redis's
        /// <c>ScriptEvaluateAsync</c> takes <c>CommandFlags</c>, not a <c>CancellationToken</c>, so
        /// per-operation cancellation does not exist to be plumbed - a limitation of the client
        /// library rather than a gap here, and such a call is bounded by <c>syncTimeout</c> anyway.
        /// </remarks>
        public async ValueTask<IReceivedMessageInternal> ReceiveMessageAsync(IMessageContext context, CancellationToken cancellation)
        {
            //These are not optional. Without them the method still compiles and still returns
            //messages, and commit and rollback silently stop working. CommitAsync is the one this
            //path actually needs: the asynchronous consumer raises that event, not Commit.
            context.Commit += _cachedCommit ??= ContextOnCommit;
            context.CommitAsync += _cachedCommitAsync ??= ContextOnCommitAsync;
            context.Rollback += _cachedRollback ??= ContextOnRollback;
            context.Cleanup += _cachedCleanup ??= Context_Cleanup;

            using (
                var workSub = _workSubFactory.Create())
            {
                while (true)
                {
                    if (_cancelWork.AnyCancellationRequested() || cancellation.IsCancellationRequested)
                    {
                        return null;
                    }

                    var message = await GetMessageAsync(context).ConfigureAwait(false);
                    if (message != null && !message.Expired)
                    {
                        return message.Message;
                    }

                    if (_cancelWork.AnyCancellationRequested() || cancellation.IsCancellationRequested)
                    {
                        return null;
                    }

                    workSub.Reset();
                    message = await GetMessageAsync(context).ConfigureAwait(false);
                    if (message != null && !message.Expired)
                    {
                        return message.Message;
                    }
                    if (message != null && message.Expired)
                    {
                        continue;
                    }
                    if (await workSub.WaitAsync(cancellation).ConfigureAwait(false))
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
        /// Gets the next message from the queue, without blocking a thread across the Lua call.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        private async Task<RedisMessage> GetMessageAsync(IMessageContext context)
        {
            var message = await _receiveMessageAsync.HandleAsync(new ReceiveMessageQuery(context)).ConfigureAwait(false);
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
        /// On Commit, awaited
        /// </summary>
        private async Task ContextOnCommitAsync(object sender, EventArgs eventArgs)
        {
            await _handleMessage.CommitMessage.CommitAsync((IMessageContext)sender).ConfigureAwait(false);
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
            context.Commit -= _cachedCommit;
            context.CommitAsync -= _cachedCommitAsync;
            context.Rollback -= _cachedRollback;
            context.Cleanup -= _cachedCleanup;
        }
    }
}
