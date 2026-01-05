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
using DotNetWorkQueue.Messages;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace DotNetWorkQueue.Trace.Decorator
{
    /// <summary>
    /// Tracer for poison messages
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IReceivePoisonMessage" />
    public class ReceivePoisonMessageDecorator : IReceivePoisonMessage
    {
        private readonly ActivitySource _tracer;
        private readonly IReceivePoisonMessage _handler;
        private readonly IStandardHeaders _headers;
        private readonly IGetHeader _getHeader;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceivePoisonMessageDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="getHeader">The get header.</param>
        public ReceivePoisonMessageDecorator(IReceivePoisonMessage handler, ActivitySource tracer, IStandardHeaders headers, IGetHeader getHeader)
        {
            _handler = handler;
            _tracer = tracer;
            _headers = headers;
            _getHeader = getHeader;
        }

        /// <inheritdoc />
        public void Handle(IMessageContext context, PoisonMessageException exception)
        {
            var header = _getHeader.GetHeaders(context.MessageId);
            if (header != null)
            {
                var activityContext = header.Extract(_tracer, _headers);
                using (var scope = _tracer.StartActivity("PoisonMessage", ActivityKind.Internal, activityContext))
                {
                    scope?.AddMessageIdTag(context);
                    scope?.AddException(exception);
                    Activity.Current?.SetStatus(ActivityStatusCode.Error);
                    _handler.Handle(context, exception);
                }
            }
            else
            {
                using (var scope = _tracer.StartActivity("PoisonMessage"))
                {
                    scope?.AddMessageIdTag(context);
                    scope?.AddException(exception);
                    _handler.Handle(context, exception);
                }
            }
        }
    }
}
