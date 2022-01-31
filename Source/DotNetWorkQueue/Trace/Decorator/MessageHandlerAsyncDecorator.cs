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

using System.Diagnostics;
using System.Threading.Tasks;
using OpenTelemetry.Trace;

namespace DotNetWorkQueue.Trace.Decorator
{
    /// <summary>
    /// Tracing for handling messages
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IMessageHandlerAsync" />
    public class MessageHandlerAsyncDecorator : IMessageHandlerAsync
    {
        private readonly IMessageHandlerAsync _handler;
        private readonly ActivitySource _tracer;
        private readonly IHeaders _headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandlerAsyncDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        public MessageHandlerAsyncDecorator(IMessageHandlerAsync handler, ActivitySource tracer, IHeaders headers)
        {
            _handler = handler;
            _tracer = tracer;
            _headers = headers;
        }

        /// <summary>
        /// Handles the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="workerNotification">The worker notification.</param>
        /// <returns></returns>
        public async Task HandleAsync(IReceivedMessageInternal message, IWorkerNotification workerNotification)
        {
            var activityContext = message.Extract(_tracer, _headers.StandardHeaders);
            using (var scope = _tracer.StartActivity("MessageHandlerAsync", ActivityKind.Internal, activityContext))
            {
                scope?.AddMessageIdTag(message);
                await _handler.HandleAsync(message, workerNotification);
            }
        }
    }
}
