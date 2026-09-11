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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Transport.Shared.Basic
{
    /// <summary>
    /// The decisions every transport makes the same way when a message fails: whether anything can be
    /// done at all, whether the exception is one that gets retried, and what happens once the message is
    /// in the error queue.
    /// </summary>
    /// <remarks>
    /// What differs between transports is only where the attempt count lives - an error-tracking table
    /// on the relational transports, the message's metadata hash on Redis - so that is all a derived
    /// class supplies.
    /// </remarks>
    public abstract class AReceiveErrorMessage : IReceiveMessagesError
    {
        private readonly QueueConsumerConfiguration _configuration;
        private readonly ILogger _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="AReceiveErrorMessage" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="log">The log.</param>
        protected AReceiveErrorMessage(QueueConsumerConfiguration configuration, ILogger log)
        {
            Guard.NotNull(configuration);
            Guard.NotNull(log);

            _configuration = configuration;
            _log = log;
        }

        /// <inheritdoc />
        public abstract ReceiveMessagesErrorResult MessageFailedProcessing(IReceivedMessageInternal message,
            IMessageContext context, Exception exception);

        /// <inheritdoc />
        public abstract Task<ReceiveMessagesErrorResult> MessageFailedProcessingAsync(IReceivedMessageInternal message,
            IMessageContext context, Exception exception);

        /// <summary>
        /// Returns false when the message has no id, which is the only case in which nothing can be done.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="exception">The exception the message failed with.</param>
        /// <param name="info">How this exception is configured to be retried.</param>
        /// <param name="exceptionType">The configured exception type, or null if none matched.</param>
        protected bool TryGetRetryInformation(IMessageContext context, Exception exception, out IRetryInformation info, out string exceptionType)
        {
            info = null;
            exceptionType = null;
            if (context.MessageId == null || !context.MessageId.HasValue) return false;

            info = _configuration.TransportConfiguration.RetryDelayBehavior.GetRetryAmount(exception);
            if (info.ExceptionType != null)
            {
                exceptionType = info.ExceptionType.ToString();
            }
            return true;
        }

        /// <summary>
        /// An exception with no configured retry behaviour goes straight to the error queue; there is
        /// no point in counting attempts for it.
        /// </summary>
        /// <param name="info">How this exception is configured to be retried.</param>
        /// <param name="exceptionType">The configured exception type, or null if none matched.</param>
        protected static bool CanRetry(IRetryInformation info, string exceptionType)
        {
            return !string.IsNullOrEmpty(exceptionType) && info.MaxRetries > 0;
        }

        /// <summary>
        /// Called once the message is in the error queue; it blocks any further action on the message and
        /// reports the failure.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="context">The context.</param>
        /// <param name="exception">The exception the message failed with.</param>
        protected ReceiveMessagesErrorResult MovedToErrorQueue(IReceivedMessageInternal message, IMessageContext context, Exception exception)
        {
            //we are done doing any processing - remove the messageID to block other actions
            context.SetMessageAndHeaders(null, context.CorrelationId, context.Headers);
            _log.LogError(exception, "Message with ID {MessageId} has failed and has been moved to the error queue", message.MessageId);
            return ReceiveMessagesErrorResult.Error;
        }
    }
}
