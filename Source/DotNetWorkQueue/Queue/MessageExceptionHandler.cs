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
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Notifications;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;
using System;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Handles action for when a message throws an exception.
    /// </summary>
    public class MessageExceptionHandler
    {
        private readonly ILogger _log;
        private readonly IReceiveMessagesError _transportErrorHandler;
        private readonly IConsumerQueueErrorNotification _consumerQueueErrorNotification;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageExceptionHandler"/> class.
        /// </summary>
        /// <param name="transportErrorHandler">The transport error handler.</param>
        /// <param name="log">The log.</param>
        /// <param name="consumerQueueErrorNotification">notifications for consumer queue errors</param>
        public MessageExceptionHandler(IReceiveMessagesError transportErrorHandler,
            ILogger log,
            IConsumerQueueErrorNotification consumerQueueErrorNotification)
        {
            Guard.NotNull(() => transportErrorHandler, transportErrorHandler);
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => consumerQueueErrorNotification, consumerQueueErrorNotification);

            _transportErrorHandler = transportErrorHandler;
            _log = log;
            _consumerQueueErrorNotification = consumerQueueErrorNotification;
        }

        /// <summary>
        /// Handles the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="context">The context.</param>
        /// <param name="exception">The exception.</param>
        /// <exception cref="DotNetWorkQueueException">An error has occurred in the error handling code</exception>
        /// <exception cref="MessageException">An unhanded exception has occurred while processing a message</exception>
        public void Handle(IReceivedMessageInternal message, IMessageContext context, Exception exception)
        {
            ReceiveMessagesErrorResult result;
            try
            {
                result = _transportErrorHandler.MessageFailedProcessing(message, context,
                    exception);
            }
            catch (Exception errorHandlingError)
            {
                _log.LogError(
                    $"An error has occurred while trying to move message {message.MessageId} to the error queue{System.Environment.NewLine}{exception}");
                throw new DotNetWorkQueueException("An error has occurred in the error handling code",
                    errorHandlingError);
            }

            switch (result)
            {
                case ReceiveMessagesErrorResult.Retry:
                case ReceiveMessagesErrorResult.NotSpecified:
                case ReceiveMessagesErrorResult.NoActionPossible:
                    throw new MessageException("An unhanded exception has occurred while processing a message",
                        exception, message.MessageId, message.CorrelationId, message.Headers);
                case ReceiveMessagesErrorResult.Error: //don't throw exception, as the message has been moved
                    _consumerQueueErrorNotification.InvokeMovedToErrorQueue(new ErrorNotification(context.MessageId, context.CorrelationId, context.Headers, exception));
                    break;
            }
        }
    }
}
