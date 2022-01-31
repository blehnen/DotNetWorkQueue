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
using System.Threading.Tasks;
using DotNetWorkQueue.Validation;
using Polly;

namespace DotNetWorkQueue.Policies.Decorator
{
    internal class ReceiveMessagesPolicyDecorator : IReceiveMessages
    {
        private readonly IReceiveMessages _handler;
        private readonly IPolicies _policies;
        private ISyncPolicy _policy;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessagesPolicyDecorator" /> class.
        /// </summary>
        public ReceiveMessagesPolicyDecorator(IPolicies policies,
            IReceiveMessages handler)
        {
            Guard.NotNull(() => policies, policies);
            Guard.NotNull(() => handler, handler);
            _policies = policies;
            _handler = handler;
        }

        /// <inheritdoc />
        public IReceivedMessageInternal ReceiveMessage(IMessageContext context)
        {
            IReceivedMessageInternal result = null;

            if (_policy == null)
                _policies.Registry.TryGet(_policies.Definition.ReceiveMessageFromTransport, out _policy);

            if (_policy != null)
            {
                _policy.Execute(() => result = _handler.ReceiveMessage(context));
            }
            else //no policy found
                result = _handler.ReceiveMessage(context);
            return result;
        }

        /// <inheritdoc />
        public bool IsBlockingOperation => _handler.IsBlockingOperation;
    }
}
