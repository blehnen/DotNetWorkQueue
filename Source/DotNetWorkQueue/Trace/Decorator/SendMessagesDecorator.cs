using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetWorkQueue.Messages;
using OpenTracing;
using OpenTracing.Tag;

namespace DotNetWorkQueue.Trace.Decorator
{
    /// <summary>
    /// Adds trace information for sending messages
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.ISendMessages" />
    public class SendMessagesDecorator : ISendMessages
    {
        private readonly ISendMessages _handler;
        private readonly ITracer _tracer;
        private readonly IHeaders _headers;
        private readonly IConnectionInformation _connectionInformation;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessagesDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public SendMessagesDecorator(ISendMessages handler, ITracer tracer, 
            IHeaders headers, IConnectionInformation connectionInformation)
        {
            _handler = handler;
            _tracer = tracer;
            _headers = headers;
            _connectionInformation = connectionInformation;
        }

        /// <inheritdoc />
        public IQueueOutputMessage Send(IMessage messageToSend, IAdditionalMessageData data)
        {
            using (IScope scope = _tracer.BuildSpan("SendMessage").StartActive(finishSpanOnDispose: true))
            {
                scope.Span.AddCommonTags(data, _connectionInformation);
                messageToSend.Inject(_tracer, scope.Span.Context, _headers.StandardHeaders);
                try
                {
                    var message = _handler.Send(messageToSend, data);
                    if(message != null)
                    {
                        if(message.HasError)
                        {
                            Tags.Error.Set(scope.Span, true);
                        }
                        else
                        {
                            scope.Span.AddMessageIdTag(message);
                        }
                    }
                    return message;
                }
                catch
                {
                    Tags.Error.Set(scope.Span, true);
                    throw;
                }
            }
        }

        /// <inheritdoc />
        public IQueueOutputMessages Send(List<QueueMessage<IMessage, IAdditionalMessageData>> messages)
        {
            //add a scope to every message
            foreach (var message in messages)
            {
                using (var scope = _tracer.BuildSpan("SendMessage").StartActive(finishSpanOnDispose: false))
                {
                    scope.Span.AddCommonTags(message.MessageData, _connectionInformation);
                    message.Message.Inject(_tracer, scope.Span.Context, _headers.StandardHeaders);
                }
            }
            return _handler.Send(messages);
        }

        /// <inheritdoc />
        public async Task<IQueueOutputMessage> SendAsync(IMessage messageToSend, IAdditionalMessageData data)
        {
            using (IScope scope = _tracer.BuildSpan("SendMessageAsync").StartActive(finishSpanOnDispose: true))
            {
                scope.Span.AddCommonTags(data, _connectionInformation);
                messageToSend.Inject(_tracer, scope.Span.Context, _headers.StandardHeaders);
                try
                {
                    var message = await _handler.SendAsync(messageToSend, data);
                    if (message != null)
                    {
                        if (message.HasError)
                        {
                            Tags.Error.Set(scope.Span, true);
                        }
                        else
                        {
                            scope.Span.AddMessageIdTag(message);
                        }
                    }
                    return message;
                }
                catch
                {
                    Tags.Error.Set(scope.Span, true);
                    throw;
                }
            }
        }

        /// <inheritdoc />
        public async Task<IQueueOutputMessages> SendAsync(List<QueueMessage<IMessage, IAdditionalMessageData>> messages)
        {
            //add a scope to every message
            foreach (var message in messages)
            {
                using (var scope = _tracer.BuildSpan("SendMessageAsync").StartActive(finishSpanOnDispose: false))
                {
                    scope.Span.AddCommonTags(message.MessageData, _connectionInformation);
                    message.Message.Inject(_tracer, scope.Span.Context, _headers.StandardHeaders);
                }
            }
            return await _handler.SendAsync(messages);
        }
    }
}
