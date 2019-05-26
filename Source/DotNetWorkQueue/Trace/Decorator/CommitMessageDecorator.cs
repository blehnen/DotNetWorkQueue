using System;
using System.Collections.Generic;
using System.Text;
using OpenTracing;

namespace DotNetWorkQueue.Trace.Decorator
{
    /// <summary>
    /// Tracer for commiting a message
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.ICommitMessage" />
    public class CommitMessageDecorator: ICommitMessage
    {
        private readonly ITracer _tracer;
        private readonly ICommitMessage _handler;
        private readonly IStandardHeaders _headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommitMessageDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        public CommitMessageDecorator(ICommitMessage handler, ITracer tracer, IStandardHeaders headers)
        {
            _handler = handler;
            _tracer = tracer;
            _headers = headers;
        }

        /// <inheritdoc />
        public bool Commit(IMessageContext context)
        {
            var spanContext = context.Extract(_tracer, _headers);
            if (spanContext != null)
            {
                using (IScope scope = _tracer.BuildSpan("Commit").AddReference(References.FollowsFrom, spanContext).StartActive(finishSpanOnDispose: true))
                {
                    return _handler.Commit(context);
                }
            }
            else
            {
                using (IScope scope = _tracer.BuildSpan("Commit").StartActive(finishSpanOnDispose: true))
                {
                    if(context.MessageId.HasValue)
                        scope.Span.SetTag("MessageID", context.MessageId.Id.Value.ToString());
                    return _handler.Commit(context);
                }
            }
        }
    }
}
