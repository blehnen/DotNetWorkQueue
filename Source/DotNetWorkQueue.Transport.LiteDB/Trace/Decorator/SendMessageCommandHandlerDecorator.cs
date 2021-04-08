// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
using DotNetWorkQueue.Trace;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Trace;
using OpenTracing;
using OpenTracing.Tag;

namespace DotNetWorkQueue.Transport.LiteDb.Trace.Decorator
{
    /// <summary>
    /// Tracing for sending a message
    /// </summary>
    public class SendMessageCommandHandlerDecorator : ICommandHandlerWithOutput<SendMessageCommand, int>
    {
        private readonly ICommandHandlerWithOutput<SendMessageCommand, int> _handler;
        private readonly ITracer _tracer;
        private readonly IHeaders _headers;
        private readonly IConnectionInformation _connectionInformation;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessageCommandHandlerDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public SendMessageCommandHandlerDecorator(ICommandHandlerWithOutput<SendMessageCommand, int> handler, ITracer tracer,
            IHeaders headers, IConnectionInformation connectionInformation)
        {
            _handler = handler;
            _tracer = tracer;
            _headers = headers;
            _connectionInformation = connectionInformation;
        }

        /// <inheritdoc />
        public int Handle(SendMessageCommand command)
        {
            using (IScope scope = _tracer.BuildSpan("SendMessage").StartActive(finishSpanOnDispose: true))
            {
                scope.Span.AddCommonTags(command.MessageData, _connectionInformation);
                scope.Span.Add(command);
                command.MessageToSend.Inject(_tracer, scope.Span.Context, _headers.StandardHeaders);
                try
                {
                    var id = _handler.Handle(command);
                    if (id == 0)
                        Tags.Error.Set(scope.Span, true);
                    scope.Span.AddMessageIdTag(id);
                    return id;
                }
                catch (Exception e)
                {
                    Tags.Error.Set(scope.Span, true);
                    scope.Span.Log(e.ToString());
                    throw;
                }
            }
        }
    }
}
