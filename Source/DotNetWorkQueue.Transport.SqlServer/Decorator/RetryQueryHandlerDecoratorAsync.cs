// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Validation;
using Polly;

namespace DotNetWorkQueue.Transport.SqlServer.Decorator
{
    /// <inheritdoc />
    /// <remarks>
    /// The asynchronous counterpart of <see cref="RetryQueryHandlerDecorator{TQuery,TResult}"/>, and it
    /// shares that decorator's pipeline - a transient read is the same transient read whichever path
    /// asked for it. Without this, the error path's retry-count read would be the one query on the
    /// asynchronous consumer with no transient-failure handling, where its synchronous twin has it.
    /// </remarks>
    internal class RetryQueryHandlerDecoratorAsync<TQuery, TResult> : IQueryHandlerAsync<TQuery, TResult> where TQuery : IQuery<TResult>
    {
        private readonly IQueryHandlerAsync<TQuery, TResult> _decorated;
        private readonly IPolicies _policies;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryQueryHandlerDecoratorAsync{TQuery,TResult}" /> class.
        /// </summary>
        /// <param name="decorated">The decorated handler.</param>
        /// <param name="policies">The policies.</param>
        public RetryQueryHandlerDecoratorAsync(IQueryHandlerAsync<TQuery, TResult> decorated,
            IPolicies policies)
        {
            Guard.NotNull(decorated);
            Guard.NotNull(policies);

            _decorated = decorated;
            _policies = policies;
        }

        /// <inheritdoc />
        public async Task<TResult> HandleAsync(TQuery query)
        {
            Guard.NotNull(query);
            ResiliencePipeline pipeline = null;
            try
            {
                _policies.Registry.TryGetPipeline(TransportPolicyDefinitions.RetryQueryHandler, out pipeline);
            }
            catch (ObjectDisposedException)
            {
                // Shutdown race: registry disposed before the last handler call.
                // Fall through to direct handler - same semantics as the "no policy" branch.
            }

            if (pipeline != null)
                return await pipeline.ExecuteAsync(async _ => await _decorated.HandleAsync(query).ConfigureAwait(false)).ConfigureAwait(false);
            return await _decorated.HandleAsync(query).ConfigureAwait(false);
        }
    }
}
