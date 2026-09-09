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
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Notifications;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

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
        private readonly IConsumerQueueErrorNotification _consumerQueueErrorNotification;
        private readonly IConsumerQueueNotification _consumerQueueNotification;
        private readonly ICancelWork _cancelWork;

        /// <summary>
        /// Occurs when message processor is idle
        /// </summary>
        public event EventHandler Idle = delegate { };
        /// <summary>
        /// Occurs when message processor is not idle
        /// </summary>
        public event EventHandler NotIdle = delegate { };

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
        /// <param name="consumerQueueErrorNotification">notifications for consumer queue errors</param>
        /// <param name="consumerQueueNotification">notifications for consumer queue messages</param>
        /// <param name="cancelWork">cancellation tokens for the queue; the receive is cancelled on stop</param>
        public MessageProcessingAsync(IReceiveMessagesFactory receiveMessages,
            IMessageContextFactory messageContextFactory,
            IQueueWaitFactory queueWaitFactory,
            ILogger log,
            ProcessMessageAsync processMessage,
            IReceivePoisonMessage receivePoisonMessage,
            IRollbackMessage rollbackMessage,
            IConsumerQueueErrorNotification consumerQueueErrorNotification,
            IConsumerQueueNotification consumerQueueNotification,
            IQueueCancelWork cancelWork)
        {
            Guard.NotNull(receiveMessages);
            Guard.NotNull(messageContextFactory);
            Guard.NotNull(queueWaitFactory);
            Guard.NotNull(log);
            Guard.NotNull(processMessage);
            Guard.NotNull(receivePoisonMessage);
            Guard.NotNull(rollbackMessage);
            Guard.NotNull(consumerQueueErrorNotification);
            Guard.NotNull(consumerQueueNotification);
            Guard.NotNull(cancelWork);

            _receiveMessages = receiveMessages;
            _messageContextFactory = messageContextFactory;
            _log = log;
            _processMessage = processMessage;
            _receivePoisonMessage = receivePoisonMessage;
            _rollbackMessage = rollbackMessage;

            _noMessageToProcessBackOffHelper = new Lazy<IQueueWait>(queueWaitFactory.CreateQueueDelay);
            _seriousExceptionProcessBackOffHelper = new Lazy<IQueueWait>(queueWaitFactory.CreateFatalErrorDelay);
            _consumerQueueErrorNotification = consumerQueueErrorNotification;
            _consumerQueueNotification = consumerQueueNotification;
            _cancelWork = cancelWork;
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

        /// <inheritdoc />
        public Task HandleAsync()
        {
            //Two different completions, and conflating them is the whole difficulty of this class.
            //The caller gets `readyForNext`, which completes once this processor can de-queue again.
            //The work itself keeps running behind that, counted in _waitingOnAsyncTasks so shutdown
            //still waits for it.
            var readyForNext = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _ = RunAsync(readyForNext);
            return readyForNext.Task;
        }

        private async Task RunAsync(TaskCompletionSource<bool> readyForNext)
        {
            Interlocked.Increment(ref _waitingOnAsyncTasks);
            try
            {
                try
                {
                    await TryProcessIncomingMessageAsync(readyForNext).ConfigureAwait(false);
                    _seriousExceptionProcessBackOffHelper.Value.Reset();
                }
                catch (CommitException exception)
                {
                    _consumerQueueErrorNotification.InvokeError(new ErrorNotification(exception.MessageId, exception.CorrelationId, exception.Headers, exception));
                }
                catch (MessageException)
                { //nothing else to do, but we want to avoid the general catch below

                }
                catch
                {
                    //generic exceptions tend to indicate a serious problem - lets start delaying processing
                    //this counter will reset once a message has been processed by this thread
                    await _seriousExceptionProcessBackOffHelper.Value.WaitAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex) //not cool - one of the exception events threw an exception
            {
                //there is not a lot we can do here - log the exception
                _log.LogError(ex, "An error has occurred while trying to handle an exception");
                _consumerQueueErrorNotification.InvokeError(new ErrorReceiveNotification(ex));
            }
            finally
            {
                //The worker loop must never be left waiting, whatever happened above - a lost signal
                //here stops that worker permanently. Setting it twice is harmless; never setting it
                //is not.
                readyForNext.TrySetResult(true);
                Interlocked.Decrement(ref _waitingOnAsyncTasks);
            }
        }
        /// <summary>
        /// Tries the process a new incoming message.
        /// </summary>
        /// <returns></returns>
        private async Task TryProcessIncomingMessageAsync(TaskCompletionSource<bool> readyForNext)
        {
            using (var context = _messageContextFactory.Create())
            {
                try
                {
                    await DoTryAsync(context, readyForNext).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex)
                {
                    _rollbackMessage.Rollback(context);
                    _consumerQueueNotification.InvokeRollback(new RollBackNotification(context.MessageId, context.CorrelationId, context.Headers, ex));
                }
                catch (PoisonMessageException exception)
                {
                    _receivePoisonMessage.Handle(context, exception);
                    _consumerQueueErrorNotification.InvokePoisonMessageError(new PoisonMessageNotification(exception));
                }
                catch (ReceiveMessageException e)
                //an exception occurred trying to get the message from the transport
                {
                    _log.LogError(e, "An error has occurred while receiving a message from the transport");
                    _consumerQueueErrorNotification.InvokeError(new ErrorReceiveNotification(e));
                    await _seriousExceptionProcessBackOffHelper.Value.WaitAsync().ConfigureAwait(false);
                }
                catch (MessageException ex)
                {
                    _rollbackMessage.Rollback(context);
                    _consumerQueueNotification.InvokeRollback(new RollBackNotification(context.MessageId, context.CorrelationId, context.Headers, ex));
                    throw;
                }
                catch (Exception ex)
                {
                    _rollbackMessage.Rollback(context);
                    _consumerQueueNotification.InvokeRollback(new RollBackNotification(context.MessageId, context.CorrelationId, context.Headers, ex));
                    throw;
                }
            }
        }

        /// <summary>
        /// Tries the process a new incoming message.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="readyForNext">
        /// Signalled once this processor can de-queue again, which releases the worker loop. Set at the
        /// point the message has been dispatched, not when its processing finishes.
        /// </param>
        /// <returns></returns>
        private async Task DoTryAsync(IMessageContext context, TaskCompletionSource<bool> readyForNext)
        {
            var receiveMessage = _receiveMessages.Create();

            //The de-queue no longer blocks. What used to cap in-flight work at N was this call
            //parking the worker's thread; that cap now lives in the worker loop, which waits on
            //readyForNext below. Awaiting here WITHOUT that would start de-queues without bound -
            //measured at 15,910,404 concurrent receives against a configured seven.
            var transportMessage = await receiveMessage.ReceiveMessageAsync(context, _cancelWork.StopWorkToken).ConfigureAwait(false);

            if (transportMessage == null)
            {
                //delay processing since we have no messages to process
                if (!_idle)
                {
                    Idle(this, EventArgs.Empty);
                    _idle = true;
                }
                //Awaited, not blocked. Before the receive became async this ran on the worker's own
                //dedicated thread, where blocking is free; now it can land in a continuation on a
                //pool thread, and an idle consumer would hold one per worker for the whole interval.
                //readyForNext stays unset until this returns, so the loop cannot spin past the
                //back-off.
                await _noMessageToProcessBackOffHelper.Value.WaitAsync().ConfigureAwait(false);
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

            //Invoking this runs synchronously all the way down through ProcessMessageAsync,
            //HandleMessage and the decorators into the user's handler - and, for the scheduler
            //consumer, into SchedulerMessageHandler.HandleAsync, whose finally blocks until the
            //scheduler has room. Only then does it return an incomplete task. That return is
            //precisely where the old `async void Handle` gave control back to the worker loop, so
            //signalling here reproduces the previous pacing exactly, throttle included.
            var processing = _processMessage.HandleAsync(context, transportMessage);
            readyForNext.TrySetResult(true);

            //Still awaited, inside the context's using: this is what keeps the error handling above
            //and the context's lifetime tied to the work completing.
            await processing.ConfigureAwait(false);
        }
    }
}
