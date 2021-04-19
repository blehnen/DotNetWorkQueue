// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// process new messages using the async handler
    /// </summary>
    public class MessageProcessingAsync : IMessageProcessing
    {
        private readonly ILogger _log;
        private readonly IReceiveMessagesFactory _receiveMessages;
        private readonly Lazy<IQueueWait> _seriousExceptionProcessBackOffHelper;
        private readonly Lazy<IQueueWait> _noMessageToProcessBackOffHelper;
        private readonly ProcessMessageAsync _processMessage;
        private readonly IMessageContextFactory _messageContextFactory;
        private readonly IReceivePoisonMessage _receivePoisonMessage;
        private readonly IRollbackMessage _rollbackMessage;

        /// <summary>
        /// Occurs when message processor is idle
        /// </summary>
        public event EventHandler Idle = delegate { };
        /// <summary>
        /// Occurs when message processor is not idle
        /// </summary>
        public event EventHandler NotIdle = delegate { };

        /// <summary>
        /// Event that will be raised each time message delivery fails.
        /// </summary>
        public event EventHandler<MessageErrorEventArgs> UserException;

        /// <summary>
        /// Event that will be raised if an exception occurs outside of user code.
        /// </summary>
        public event EventHandler<MessageErrorEventArgs> SystemException;

        private bool _idle;
        private long _waitingOnAsyncTasks;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageProcessingAsync"/> class.
        /// </summary>
        /// <param name="receiveMessages">The receive messages.</param>
        /// <param name="messageContextFactory">The message context factory.</param>
        /// <param name="queueWaitFactory">The queue wait factory.</param>
        /// <param name="log">The log.</param>
        /// <param name="processMessage">The process message.</param>
        /// <param name="receivePoisonMessage">The receive poison message.</param>
        /// <param name="rollbackMessage">rolls back a message when an exception occurs</param>
        public MessageProcessingAsync(IReceiveMessagesFactory receiveMessages,
            IMessageContextFactory messageContextFactory,
            IQueueWaitFactory queueWaitFactory,
            ILogger log,
            ProcessMessageAsync processMessage,
            IReceivePoisonMessage receivePoisonMessage, 
            IRollbackMessage rollbackMessage)
        {
            Guard.NotNull(() => receiveMessages, receiveMessages);
            Guard.NotNull(() => messageContextFactory, messageContextFactory);
            Guard.NotNull(() => queueWaitFactory, queueWaitFactory);
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => processMessage, processMessage);
            Guard.NotNull(() => receivePoisonMessage, receivePoisonMessage);
            Guard.NotNull(() => rollbackMessage, rollbackMessage);

            _receiveMessages = receiveMessages;
            _messageContextFactory = messageContextFactory;
            _log = log;
            _processMessage = processMessage;
            _receivePoisonMessage = receivePoisonMessage;
            _rollbackMessage = rollbackMessage;

            _noMessageToProcessBackOffHelper = new Lazy<IQueueWait>(queueWaitFactory.CreateQueueDelay);
            _seriousExceptionProcessBackOffHelper = new Lazy<IQueueWait>(queueWaitFactory.CreateFatalErrorDelay);
        }

        /// <summary>
        /// Returns how many asynchronous tasks are still running.
        /// </summary>
        /// <value>
        /// How many asynchronous tasks are still running.
        /// </value>
        /// <remarks>
        /// Used when shutting down the queue. We cannot cleanly shut down unless this value is zero.
        /// </remarks>
        public long AsyncTaskCount => Interlocked.Read(ref _waitingOnAsyncTasks);

        /// <summary>
        /// Looks for and processes a new message
        /// </summary>
        public async void Handle()
        {
            Interlocked.Increment(ref _waitingOnAsyncTasks);
            try
            {
                try
                {
                    await TryProcessIncomingMessageAsync().ConfigureAwait(false);
                    _seriousExceptionProcessBackOffHelper.Value.Reset();
                }
                catch (MessageException exception)
                {
                    UserException?.Invoke(this, new MessageErrorEventArgs(exception));
                }
                catch (CommitException exception)
                {
                    UserException?.Invoke(this, new MessageErrorEventArgs(exception));
                }
                catch (Exception e)
                {
                    SystemException?.Invoke(this, new MessageErrorEventArgs(e));

                    //generic exceptions tend to indicate a serious problem - lets start delaying processing
                    //this counter will reset once a message has been processed by this thread
                    _seriousExceptionProcessBackOffHelper.Value.Wait();
                }
            }
            catch (Exception ex) //not cool - one of the exception events threw an exception
            {
                //there is not a lot we can do here - log the exception
                _log.LogError("An error has occurred while trying to handle an exception", ex);
            }
            finally
            {
                Interlocked.Decrement(ref _waitingOnAsyncTasks);
            }
        }
        /// <summary>
        /// Tries the process a new incoming message.
        /// </summary>
        /// <returns></returns>
        private async Task TryProcessIncomingMessageAsync()
        {
            using (var context = _messageContextFactory.Create())
            {
                try
                {
                    await DoTryAsync(context).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _rollbackMessage.Rollback(context);
                }
                // ReSharper disable once UncatchableException
                catch (ThreadAbortException)
                {
                    _rollbackMessage.Rollback(context);
                }
                catch (PoisonMessageException exception)
                {
                    _receivePoisonMessage.Handle(context, exception);
                }
                catch (ReceiveMessageException e)
                //an exception occurred trying to get the message from the transport
                {
                    _log.LogError("An error has occurred while receiving a message from the transport", e);
                    _seriousExceptionProcessBackOffHelper.Value.Wait();
                }
                catch
                {
                    _rollbackMessage.Rollback(context);
                    throw;
                }
            }
        }

        /// <summary>
        /// Tries the process a new incoming message.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        private async Task DoTryAsync(IMessageContext context)
        {
            var receiveMessage = _receiveMessages.Create();

            //this call must block; otherwise, the limitations enforced by the task scheduler will be ignored
            //calling the async method here may result in hundreds of de-queues at once, instead of the N set in the configuration
            var transportMessage = receiveMessage.ReceiveMessage(context);

            if (transportMessage == null)
            {
                //delay processing since we have no messages to process
                if (!_idle)
                {
                    Idle(this, EventArgs.Empty);
                    _idle = true;
                }
                _noMessageToProcessBackOffHelper.Value.Wait();
                return;
            }

            //reset the back off counter - we have a message to process, so the wait times are now reset
            _noMessageToProcessBackOffHelper.Value.Reset();
            //we are no longer idle
            if (_idle)
            {
                NotIdle(this, EventArgs.Empty);
                _idle = false;
            }

            //process the message
            await _processMessage.HandleAsync(context, transportMessage).ConfigureAwait(false);
        }
    }
}
