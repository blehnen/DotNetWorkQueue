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
using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Runs user code and commits the message
    /// </summary>
    public class ProcessMessageAsync
    {
        private readonly IHandleMessage _methodToRun;
        private readonly MessageExceptionHandler _messageExceptionHandler;
        private readonly IHeartBeatWorkerFactory _heartBeatWorkerFactory;
        private readonly ICommitMessage _commitMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessMessageAsync"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="heartBeatWorkerFactory">The heart beat worker factory.</param>
        /// <param name="messageExceptionHandler">The message exception handler.</param>
        /// <param name="commitMessage">The commit message.</param>
        public ProcessMessageAsync(IHandleMessage handler,
            IHeartBeatWorkerFactory heartBeatWorkerFactory,
            MessageExceptionHandler messageExceptionHandler,
            ICommitMessage commitMessage)
        {
            Guard.NotNull(() => handler, handler);
            Guard.NotNull(() => heartBeatWorkerFactory, heartBeatWorkerFactory);
            Guard.NotNull(() => messageExceptionHandler, messageExceptionHandler);
            Guard.NotNull(() => commitMessage, commitMessage);

            _messageExceptionHandler = messageExceptionHandler;
            _methodToRun = handler;
            _heartBeatWorkerFactory = heartBeatWorkerFactory;
            _commitMessage = commitMessage;
        }

        /// <summary>
        /// Handles processing the specified message with the context
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="transportMessage">The transport message.</param>
        /// <returns></returns>
        public async Task HandleAsync(IMessageContext context, IReceivedMessageInternal transportMessage)
        {
            using (var heartBeat = _heartBeatWorkerFactory.Create(context))
            {
                try
                {
                    await _methodToRun.HandleAsync(transportMessage, context.WorkerNotification).ConfigureAwait(false);
                    _commitMessage.Commit(context);
                }
                // ReSharper disable once UncatchableException
                catch (ThreadAbortException)
                {
                    heartBeat.Stop();
                    throw;
                }
                catch (OperationCanceledException)
                {
                    heartBeat.Stop();
                    throw;
                }
                catch (Exception exception)
                {
                    heartBeat.Stop();
                    _messageExceptionHandler.Handle(transportMessage, context, exception.InnerException ?? exception);
                }
            }
        }
    }
}
