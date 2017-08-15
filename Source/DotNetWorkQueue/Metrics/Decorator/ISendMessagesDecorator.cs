// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue.Metrics.Decorator
{
    internal class SendMessagesDecorator: ISendMessages
    {
        private readonly ITimer _sendTimer;
        private readonly ITimer _sendBatchTimer;
        private readonly ITimer _sendAsyncTimer;
        private readonly ITimer _sendBatchAsyncTimer;

        private readonly IMeter _sendMeter;
        private readonly IMeter _sendErrorMeter;
        private readonly ISendMessages _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessagesDecorator" /> class.
        /// </summary>
        /// <param name="metrics">The metrics factory.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public SendMessagesDecorator(IMetrics metrics,
            ISendMessages handler,
            IConnectionInformation connectionInformation)
        {
            var name = handler.GetType().Name;
            _sendTimer = metrics.Timer($"{connectionInformation.QueueName}.{name}.SendTimer", Units.Calls);
            _sendBatchTimer = metrics.Timer($"{connectionInformation.QueueName}.{name}.SendBatchTimer", Units.Calls);
            _sendAsyncTimer = metrics.Timer($"{connectionInformation.QueueName}.{name}.SendAsyncTimer", Units.Calls);
            _sendBatchAsyncTimer = metrics.Timer($"{connectionInformation.QueueName}.{name}.SendBatchAsyncTimer", Units.Calls);

            _sendMeter = metrics.Meter($"{connectionInformation.QueueName}.{name}.SendMessagesMeter", Units.Items, TimeUnits.Minutes);
            _sendErrorMeter = metrics.Meter($"{connectionInformation.QueueName}.{name}.SendMessagesErrorMeter", Units.Items, TimeUnits.Minutes);
            _handler = handler;
        }

        /// <summary>
        /// Sends a new message to an existing queue
        /// </summary>
        /// <param name="messageToSend">The message to send.</param>
        /// <param name="data">The additional data.</param>
        /// <returns></returns>
        public IQueueOutputMessage Send(IMessage messageToSend, IAdditionalMessageData data)
        {
            using (_sendTimer.NewContext())
            {
                var result = _handler.Send(messageToSend, data);
                if (!result.HasError)
                {
                    _sendMeter.Mark("SendMessage", 1);
                }
                else
                {
                    _sendErrorMeter.Mark("SendMessage", 1);
                }
                return result;
            }
        }

        /// <summary>
        /// Sends a collection of new messages to an existing queue
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns></returns>
        public IQueueOutputMessages Send(List<QueueMessage<IMessage, IAdditionalMessageData>> messages)
        {
            using (_sendBatchTimer.NewContext())
            {
                var result = _handler.Send(messages);
                 _sendMeter.Mark("SendMessageBatch", result.Count(x => !x.HasError));
                 _sendErrorMeter.Mark("SendMessageBatch", result.Count(x => x.HasError));
                return result;
            }
        }

        /// <summary>
        /// Sends a new message to an existing queue
        /// </summary>
        /// <param name="messageToSend">The message to send.</param>
        /// <param name="data">The additional data.</param>
        /// <returns></returns>
        public async Task<IQueueOutputMessage> SendAsync(IMessage messageToSend, IAdditionalMessageData data)
        {
            using (_sendAsyncTimer.NewContext())
            {
                var result = await _handler.SendAsync(messageToSend, data).ConfigureAwait(false);
                if (!result.HasError)
                {
                    _sendMeter.Mark("SendMessageAsync", 1);
                }
                else
                {
                    _sendErrorMeter.Mark("SendMessageAsync", 1);
                }
                return result;
            }
        }

        /// <summary>
        /// Sends a collection of new messages to an existing queue
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns></returns>
        public async Task<IQueueOutputMessages> SendAsync(List<QueueMessage<IMessage, IAdditionalMessageData>> messages)
        {
            using (_sendBatchAsyncTimer.NewContext())
            {
                var result = await _handler.SendAsync(messages).ConfigureAwait(false);
                _sendMeter.Mark("SendMessageBatchAsync", result.Count(x => !x.HasError));
                _sendErrorMeter.Mark("SendMessageBatchAsync", result.Count(x => x.HasError));
                return result;
            }
        }
    }
}
