// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Trace;
using OpenTelemetry.Trace;

namespace DotNetWorkQueue.Transport.Redis.Trace.Decorator
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.ISendMessages" />
    public class SendMessagesDecorator: ISendMessages
    {
        private readonly ISendMessages _handler;
        private readonly ActivitySource _tracer;
        private readonly IHeaders _headers;
        private readonly IConnectionInformation _connectionInformation;


        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessagesDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public SendMessagesDecorator(ISendMessages handler, ActivitySource tracer, IHeaders headers, IConnectionInformation connectionInformation)
        {
            _handler = handler;
            _tracer = tracer;
            _headers = headers;
            _connectionInformation = connectionInformation;
        }

        /// <summary>
        /// Sends a new message to an existing queue
        /// </summary>
        /// <param name="messageToSend">The message to send.</param>
        /// <param name="data">The additional data.</param>
        /// <returns></returns>
        public IQueueOutputMessage Send(IMessage messageToSend, IAdditionalMessageData data)
        {
            using (var scope = _tracer.StartActivity("SendMessage"))
            {
                scope?.AddCommonTags(data, _connectionInformation);
                scope?.Add(data);
                scope?.SetTag("IsBatch", false);
                if(scope != null)
                    messageToSend.Inject(_tracer, scope.Context, _headers.StandardHeaders);
                try
                {
                    var outputMessage = _handler.Send(messageToSend, data);
                    if (outputMessage.HasError)
                    {
                        scope?.SetStatus(Status.Error);
                        if (outputMessage.SendingException != null)
                            scope?.RecordException(outputMessage.SendingException);
                    }
                    scope?.AddMessageIdTag(outputMessage.SentMessage.MessageId);
                    return outputMessage;
                }
                catch (Exception e)
                {
                    scope?.SetStatus(Status.Error);
                    scope?.RecordException(e);
                    throw;
                }
            }
        }

        /// <summary>
        /// Sends a collection of new messages to an existing queue
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns></returns>
        public IQueueOutputMessages Send(List<QueueMessage<IMessage, IAdditionalMessageData>> messages)
        {
            foreach (var message in messages)
            {
                using (var scope = _tracer.StartActivity("SendMessage"))
                {
                    scope?.AddCommonTags(message.MessageData, _connectionInformation);
                    scope?.Add(message.MessageData);
                    scope?.SetTag("IsBatch", true);
                    if(scope != null)
                        message.Message.Inject(_tracer, scope.Context, _headers.StandardHeaders);
                }
            }
            return _handler.Send(messages);
        }

        /// <summary>
        /// Sends a new message to an existing queue
        /// </summary>
        /// <param name="messageToSend">The message to send.</param>
        /// <param name="data">The additional data.</param>
        /// <returns></returns>
        public async Task<IQueueOutputMessage> SendAsync(IMessage messageToSend, IAdditionalMessageData data)
        {
            //lets add a bit more information to the active span if possible
            using (var scope = _tracer.StartActivity("SendMessage"))
            {
                scope?.AddCommonTags(data, _connectionInformation);
                scope?.Add(data);
                scope?.SetTag("IsBatch", false);
                if(scope?.Context != null)
                    messageToSend.Inject(_tracer, scope.Context, _headers.StandardHeaders);
                try
                {
                    var outputMessage = await _handler.SendAsync(messageToSend, data);
                    if (outputMessage.HasError)
                    {
                        scope?.SetStatus(Status.Error);
                        if (outputMessage.SendingException != null)
                            scope?.RecordException(outputMessage.SendingException);
                    }
                    scope?.AddMessageIdTag(outputMessage.SentMessage.MessageId);
                    return outputMessage;
                }
                catch (Exception e)
                {
                    scope?.SetStatus(Status.Error);
                    scope?.RecordException(e);
                    throw;
                }
            }
        }

        /// <summary>
        /// Sends a collection of new messages to an existing queue
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns></returns>
        public async Task<IQueueOutputMessages> SendAsync(List<QueueMessage<IMessage, IAdditionalMessageData>> messages)
        {
            foreach (var message in messages)
            {
                using (var scope = _tracer.StartActivity("SendMessage"))
                {
                    scope?.AddCommonTags(message.MessageData, _connectionInformation);
                    scope?.Add(message.MessageData);
                    scope?.SetTag("IsBatch", true);
                    if(scope?.Context != null)
                        message.Message.Inject(_tracer, scope.Context, _headers.StandardHeaders);
                }
            }
            return await _handler.SendAsync(messages);
        }
    }
}
