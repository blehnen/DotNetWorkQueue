// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
    internal class RetryQueryHandlerDecorator<TQuery, TResult> : IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
    {
        private readonly IQueryHandler<TQuery, TResult> _decorated;
        private readonly IPolicies _policies;
        private ISyncPolicy _policy;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryQueryHandlerDecorator{TQuery,TResult}" /> class.
        /// </summary>
        /// <param name="decorated">The decorated.</param>
        /// <param name="policies">The policies.</param>
        public RetryQueryHandlerDecorator(IQueryHandler<TQuery, TResult> decorated,
            IPolicies policies)
        {
            Guard.NotNull(() => decorated, decorated);
            Guard.NotNull(() => policies, policies);

            _decorated = decorated;
            _policies = policies;
        }

        /// <inheritdoc />
        public TResult Handle(TQuery query)
        {
            Guard.NotNull(() => query, query);
            if (_policy == null)
            {
                _policies.Registry.TryGet(TransportPolicyDefinitions.RetryQueryHandler, out _policy);
            }
            if (_policy != null)
            {
                var result = _policy.ExecuteAndCapture(() => _decorated.Handle(query));
                if (result.FinalException != null)
                    throw result.FinalException;
                return result.Result;
            }
            return _decorated.Handle(query);
        }
    }
}
