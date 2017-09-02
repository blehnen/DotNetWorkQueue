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
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Handles action for when a message throws an exception.
    /// </summary>
    public class MessageExceptionHandler
    {
        private readonly ILog _log;
        private readonly IReceiveMessagesError _transportErrorHandler;
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageExceptionHandler"/> class.
        /// </summary>
        /// <param name="transportErrorHandler">The transport error handler.</param>
        /// <param name="log">The log.</param>
        public MessageExceptionHandler(IReceiveMessagesError transportErrorHandler, 
            ILogFactory log)
        {
            Guard.NotNull(() => transportErrorHandler, transportErrorHandler);
            Guard.NotNull(() => log, log);
            _transportErrorHandler = transportErrorHandler;
            _log = log.Create();
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
            try
            {
                _transportErrorHandler.MessageFailedProcessing(message, context,
                    exception);
            }
            catch (Exception errorHandlingError)
            {
                _log.ErrorException(
                    "An error has occurred while trying to move message {0} to the error queue", exception,
                    message.MessageId);
                throw new DotNetWorkQueueException("An error has occurred in the error handling code",
                    errorHandlingError);
            }
            throw new MessageException("An unhanded exception has occurred while processing a message",
                exception, message.MessageId, message.CorrelationId);
        }
    }
}
