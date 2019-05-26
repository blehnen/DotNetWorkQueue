// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
using OpenTracing;

namespace DotNetWorkQueue.Trace.Decorator
{
    /// <summary>
    /// Sends trace information for sending heartbeats
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.ISendHeartBeat" />
    public class SendHeartBeatDecorator: ISendHeartBeat
    {
        private readonly ITracer _tracer;
        private readonly ISendHeartBeat _handler;
        private readonly IStandardHeaders _headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendHeartBeatDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        public SendHeartBeatDecorator(ISendHeartBeat handler, ITracer tracer, IStandardHeaders headers)
        {
            _handler = handler;
            _tracer = tracer;
            _headers = headers;
        }

        /// <inheritdoc />
        public IHeartBeatStatus Send(IMessageContext context)
        {
            var spanContext = context.Extract(_tracer, _headers);
            if (spanContext != null)
            {
                using (IScope scope = _tracer.BuildSpan("SendHeartBeat").AddReference(References.FollowsFrom, spanContext).StartActive(finishSpanOnDispose: true))
                {
                    var status = _handler.Send(context);
                    if (status.LastHeartBeatTime.HasValue)
                        scope.Span.SetTag("HeartBeatValue", status.LastHeartBeatTime.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    return status;
                }
            }
            else
            {
                using (IScope scope = _tracer.BuildSpan("SendHeartBeat").StartActive(finishSpanOnDispose: true))
                {
                    scope.Span.AddMessageIdTag(context);
                    var status = _handler.Send(context);
                    if (status.LastHeartBeatTime.HasValue)
                        scope.Span.SetTag("HeartBeatValue", status.LastHeartBeatTime.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    return status;
                }
            }
        }
    }
}
