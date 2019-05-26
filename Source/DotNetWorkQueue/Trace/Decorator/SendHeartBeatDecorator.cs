using System;
using System.Collections.Generic;
using System.Text;
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
