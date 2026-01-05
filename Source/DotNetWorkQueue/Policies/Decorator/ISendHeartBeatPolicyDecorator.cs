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
using Polly;

namespace DotNetWorkQueue.Policies.Decorator
{
    internal class SendHeartBeatPolicyDecorator : ISendHeartBeat
    {
        private readonly ISendHeartBeat _handler;
        private readonly IPolicies _policies;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendHeartBeatPolicyDecorator" /> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="policies">The policies.</param>
        public SendHeartBeatPolicyDecorator(
            ISendHeartBeat handler,
            IPolicies policies)
        {
            _policies = policies;
            _handler = handler;
        }

        /// <inheritdoc />
        public IHeartBeatStatus Send(IMessageContext context)
        {
            IHeartBeatStatus result = null;
            if (_policies.Registry.TryGet<ISyncPolicy>(_policies.Definition.SendHeartBeat, out var policy))
            {
                policy.Execute(() => result = _handler.Send(context));
            }
            else //no policy found
                result = _handler.Send(context);
            return result;
        }
    }
}
