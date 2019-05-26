using System.Collections.Generic;
using System.Threading;
using OpenTracing;

namespace DotNetWorkQueue.Trace.Decorator
{
    /// <summary>
    /// Tracer for heart beat resetting
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IResetHeartBeat" />
    public class ResetHeartBeatDecorator: IResetHeartBeat
    {
        private readonly ITracer _tracer;
        private readonly IResetHeartBeat _handler;
        private readonly IStandardHeaders _headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResetHeartBeatDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        public ResetHeartBeatDecorator(IResetHeartBeat handler, ITracer tracer, IStandardHeaders headers)
        {
            _handler = handler;
            _tracer = tracer;
            _headers = headers;
        }

        /// <inheritdoc />
        public List<ResetHeartBeatOutput> Reset(CancellationToken cancelToken)
        {
            var results = _handler.Reset(cancelToken);
            foreach (var result in results)
            {
                if (result.Headers != null)
                {
                    var spanContext = result.Headers.Extract(_tracer, _headers);
                    if (spanContext != null)
                    {
                        IScope scope = _tracer.BuildSpan("ResetHeartBeat")
                            .AddReference(References.FollowsFrom, spanContext).WithStartTimestamp(result.ApproximateResetTimeStart).StartActive(finishSpanOnDispose: true);
                        scope.Span.Finish(result.ApproximateResetTimeEnd);
                    }
                    else
                    {
                        using (IScope scope = _tracer.BuildSpan("ResetHeartBeat").WithStartTimestamp(result.ApproximateResetTimeStart).StartActive(finishSpanOnDispose: true))
                        {
                            if(result.MessageId.HasValue)
                                scope.Span.SetTag("MessageID", result.MessageId.Id.Value.ToString());
                            scope.Span.Finish(result.ApproximateResetTimeEnd);
                        }
                    }
                }
                else
                {
                    using (IScope scope = _tracer.BuildSpan("ResetHeartBeat").WithStartTimestamp(result.ApproximateResetTimeStart).StartActive(finishSpanOnDispose: true))
                    {
                        if (result.MessageId.HasValue)
                            scope.Span.SetTag("MessageID", result.MessageId.Id.Value.ToString());
                        scope.Span.Finish(result.ApproximateResetTimeEnd);
                    }
                }
            }

            return results;
        }
    }
}
