using System;
using DotNetWorkQueue.Trace;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using OpenTracing;
using OpenTracing.Tag;
using DotNetWorkQueue.Transport.RelationalDatabase.Trace;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Trace.Decorator
{
    public class SendMessageCommandHandlerDecorator : ICommandHandlerWithOutput<SendMessageCommand, long>
    {
        private readonly ICommandHandlerWithOutput<SendMessageCommand, long> _handler;
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
        public SendMessageCommandHandlerDecorator(ICommandHandlerWithOutput<SendMessageCommand, long> handler, ITracer tracer,
            IHeaders headers, IConnectionInformation connectionInformation)
        {
            _handler = handler;
            _tracer = tracer;
            _headers = headers;
            _connectionInformation = connectionInformation;
        }

        /// <inheritdoc />
        public long Handle(SendMessageCommand command)
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
