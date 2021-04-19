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
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;
using Polly;

namespace DotNetWorkQueue.Transport.PostgreSQL.Decorator
{
    /// <inheritdoc />
    internal class RetryCommandHandlerDecorator<TCommand> : ICommandHandler<TCommand>
    {
        private readonly ICommandHandler<TCommand> _decorated;
        private readonly IPolicies _policies;
        private ISyncPolicy _policy;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryCommandHandlerDecorator{TCommand}" /> class.
        /// </summary>
        /// <param name="decorated">The decorated handler.</param>
        /// <param name="policies">The policies.</param>
        public RetryCommandHandlerDecorator(ICommandHandler<TCommand> decorated,
            IPolicies policies)
        {
            Guard.NotNull(() => decorated, decorated);
            Guard.NotNull(() => policies, policies);

            _decorated = decorated;
            _policies = policies;
        }

        /// <inheritdoc />
        public void Handle(TCommand command)
        {
            if (_policy == null)
            {
                _policies.Registry.TryGet(TransportPolicyDefinitions.RetryCommandHandler, out _policy);
            }
            if (_policy != null)
            {
                _policy.Execute(() => _decorated.Handle(command));
            }
            else //no policy found
                _decorated.Handle(command);
        }
    }
}
