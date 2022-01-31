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
using System;
using System.Diagnostics;
using OpenTelemetry.Trace;
namespace DotNetWorkQueue.Trace.Decorator
{
    /// <summary>
    /// Tracer for receiving a message
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IReceiveMessagesError" />
    public class ReceiveMessagesErrorDecorator : IReceiveMessagesError
    {
        private readonly ActivitySource _tracer;
        private readonly IReceiveMessagesError _handler;
        private readonly IStandardHeaders _headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessagesErrorDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        public ReceiveMessagesErrorDecorator(IReceiveMessagesError handler, ActivitySource tracer, IStandardHeaders headers)
        {
            _handler = handler;
            _tracer = tracer;
            _headers = headers;
        }

        /// <inheritdoc />
        public ReceiveMessagesErrorResult MessageFailedProcessing(IReceivedMessageInternal message, IMessageContext context, Exception exception)
        {
            var activityContext = message.Extract(_tracer, _headers);
            using (var scope = _tracer.StartActivity("Error", ActivityKind.Internal, parentContext: activityContext))
            {
                scope?.AddMessageIdTag(message);
                scope?.RecordException(exception);
                scope?.SetStatus(Status.Error); ;
                var result = _handler.MessageFailedProcessing(message, context, exception);
                scope?.SetTag("WillRetry", result == ReceiveMessagesErrorResult.Retry);
                return result;
            }
        }
    }
}
