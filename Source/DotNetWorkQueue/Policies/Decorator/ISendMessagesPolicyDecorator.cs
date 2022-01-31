// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using System.Threading.Tasks;
using DotNetWorkQueue.Messages;
using Polly;

namespace DotNetWorkQueue.Policies.Decorator
{
    internal class SendMessagesPolicyDecorator : ISendMessages
    {
        private readonly IPolicies _policies;
        private ISyncPolicy _policy;
        private IAsyncPolicy _policyAsync;
        private readonly ISendMessages _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessagesPolicyDecorator" /> class.
        /// </summary>
        /// <param name="policies">The policies.</param>
        /// <param name="handler">The handle.</param>
        public SendMessagesPolicyDecorator(IPolicies policies,
            ISendMessages handler)
        {
            _policies = policies;
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
            IQueueOutputMessage result = null;
            if (_policy == null)
                _policies.Registry.TryGet(_policies.Definition.SendMessage, out _policy);

            if (_policy != null)
            {
                _policy.Execute(() => result = _handler.Send(messageToSend, data));
            }
            else //no policy found
                result = _handler.Send(messageToSend, data);
            return result;
        }

        /// <summary>
        /// Sends a collection of new messages to an existing queue
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns></returns>
        public IQueueOutputMessages Send(List<QueueMessage<IMessage, IAdditionalMessageData>> messages)
        {
            IQueueOutputMessages result = null;
            if (_policy == null)
                _policies.Registry.TryGet(_policies.Definition.SendMessage, out _policy);

            if (_policy != null)
            {
                _policy.Execute(() => result = _handler.Send(messages));
            }
            else //no policy found
                result = _handler.Send(messages);
            return result;
        }

        /// <summary>
        /// Sends a new message to an existing queue
        /// </summary>
        /// <param name="messageToSend">The message to send.</param>
        /// <param name="data">The additional data.</param>
        /// <returns></returns>
        public async Task<IQueueOutputMessage> SendAsync(IMessage messageToSend, IAdditionalMessageData data)
        {
            IQueueOutputMessage result = null;
            if (_policyAsync == null)
                _policies.Registry.TryGet(_policies.Definition.SendMessageAsync, out _policyAsync);

            if (_policyAsync != null)
            {
                await _policyAsync.ExecuteAsync(async () => result = await _handler.SendAsync(messageToSend, data).ConfigureAwait(false)).ConfigureAwait(false);
            }
            else //no policy found
                result = await _handler.SendAsync(messageToSend, data).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Sends a collection of new messages to an existing queue
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns></returns>
        public async Task<IQueueOutputMessages> SendAsync(List<QueueMessage<IMessage, IAdditionalMessageData>> messages)
        {
            IQueueOutputMessages result = null;
            if (_policyAsync == null)
                _policies.Registry.TryGet(_policies.Definition.SendMessageAsync, out _policyAsync);

            if (_policyAsync != null)
            {
                await _policyAsync.ExecuteAsync(async () => result = await _handler.SendAsync(messages).ConfigureAwait(false)).ConfigureAwait(false);
            }
            else //no policy found
                result = await _handler.SendAsync(messages).ConfigureAwait(false);

            return result;
        }
    }
}
