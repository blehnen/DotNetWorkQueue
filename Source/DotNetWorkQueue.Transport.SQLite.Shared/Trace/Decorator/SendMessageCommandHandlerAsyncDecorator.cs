using System;
using System.Threading.Tasks;
using DotNetWorkQueue.Trace;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using OpenTracing;
using OpenTracing.Tag;
using DotNetWorkQueue.Transport.RelationalDatabase.Trace;
using DotNetWorkQueue.Transport.Shared;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Trace.Decorator
{
    /// <summary>
    /// 
    /// </summary>
    public class SendMessageCommandHandlerAsyncDecorator : ICommandHandlerWithOutputAsync<SendMessageCommand, long>
    {
        private readonly ICommandHandlerWithOutputAsync<SendMessageCommand, long> _handler;
        private readonly ITracer _tracer;
        private readonly IHeaders _headers;
        private readonly IConnectionInformation _connectionInformation;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessageCommandHandlerAsyncDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public SendMessageCommandHandlerAsyncDecorator(ICommandHandlerWithOutputAsync<SendMessageCommand, long> handler, ITracer tracer,
            IHeaders headers, IConnectionInformation connectionInformation)
        {
            _handler = handler;
            _tracer = tracer;
            _headers = headers;
            _connectionInformation = connectionInformation;
        }


        /// <inheritdoc />
        public async Task<long> HandleAsync(SendMessageCommand command)
        {
            using (IScope scope = _tracer.BuildSpan("SendMessage").StartActive(finishSpanOnDispose: true))
            {
                scope.Span.AddCommonTags(command.MessageData, _connectionInformation);
                scope.Span.Add(command);
                command.MessageToSend.Inject(_tracer, scope.Span.Context, _headers.StandardHeaders);
                try
                {
                    var id = await _handler.HandleAsync(command);
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
