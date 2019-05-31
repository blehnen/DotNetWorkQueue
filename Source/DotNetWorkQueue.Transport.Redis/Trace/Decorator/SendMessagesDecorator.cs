using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Trace;
using OpenTracing;
using OpenTracing.Tag;

namespace DotNetWorkQueue.Transport.Redis.Trace.Decorator
{
    public class SendMessagesDecorator: ISendMessages
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
        public SendMessagesDecorator(ISendMessages handler, ITracer tracer, IHeaders headers, IConnectionInformation connectionInformation)
        {
            _handler = handler;
            _tracer = tracer;
            _headers = headers;
            _connectionInformation = connectionInformation;
        }

        public IQueueOutputMessage Send(IMessage messageToSend, IAdditionalMessageData data)
        {
            using (IScope scope = _tracer.BuildSpan("SendMessage").StartActive(finishSpanOnDispose: true))
            {
                scope.Span.AddCommonTags(data, _connectionInformation);
                scope.Span.Add(data);
                messageToSend.Inject(_tracer, scope.Span.Context, _headers.StandardHeaders);
                try
                {
                    var outputMessage = _handler.Send(messageToSend, data);
                    if (outputMessage.HasError)
                    {
                        Tags.Error.Set(scope.Span, true);
                        if (outputMessage.SendingException != null)
                            scope.Span.Log(outputMessage.SendingException.ToString());
                    }
                    scope.Span.AddMessageIdTag(outputMessage.SentMessage.MessageId);
                    return outputMessage;
                }
                catch (Exception e)
                {
                    Tags.Error.Set(scope.Span, true);
                    scope.Span.Log(e.ToString());
                    throw;
                }
            }
        }

        public IQueueOutputMessages Send(List<QueueMessage<IMessage, IAdditionalMessageData>> messages)
        {
            //TODO
            return _handler.Send(messages);
        }

        public async Task<IQueueOutputMessage> SendAsync(IMessage messageToSend, IAdditionalMessageData data)
        {
            //lets add a bit more information to the active span if possible
            using (IScope scope = _tracer.BuildSpan("SendMessage").StartActive(finishSpanOnDispose: true))
            {
                scope.Span.AddCommonTags(data, _connectionInformation);
                scope.Span.Add(data);
                messageToSend.Inject(_tracer, scope.Span.Context, _headers.StandardHeaders);
                try
                {
                    var outputMessage = await _handler.SendAsync(messageToSend, data);
                    if (outputMessage.HasError)
                    {
                        Tags.Error.Set(scope.Span, true);
                        if (outputMessage.SendingException != null)
                            scope.Span.Log(outputMessage.SendingException.ToString());
                    }
                    scope.Span.AddMessageIdTag(outputMessage.SentMessage.MessageId);
                    return outputMessage;
                }
                catch (Exception e)
                {
                    Tags.Error.Set(scope.Span, true);
                    scope.Span.Log(e.ToString());
                    throw;
                }
            }
        }

        public async Task<IQueueOutputMessages> SendAsync(List<QueueMessage<IMessage, IAdditionalMessageData>> messages)
        {
            //TODO
            return await _handler.SendAsync(messages);
        }
    }
}
