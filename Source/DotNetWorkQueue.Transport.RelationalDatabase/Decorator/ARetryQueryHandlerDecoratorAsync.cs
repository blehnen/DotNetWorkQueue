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
using DotNetWorkQueue.Validation;
using Polly;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Decorator
{
    /// <summary>
    /// Runs an asynchronous query through the transport's retry-query pipeline, so a transient failure
    /// is retried rather than surfacing to the caller.
    /// </summary>
    /// <remarks>
    /// The synchronous <c>RetryQueryHandlerDecorator</c> is duplicated per transport, differing only in
    /// which pipeline name it looks up. The asynchronous one is shared instead, with the name supplied
    /// by <see cref="PolicyName"/> - a transient read is the same transient read on every transport.
    /// </remarks>
    /// <typeparam name="TQuery">The query.</typeparam>
    /// <typeparam name="TResult">What the query returns.</typeparam>
    public abstract class ARetryQueryHandlerDecoratorAsync<TQuery, TResult> : IQueryHandlerAsync<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private readonly IQueryHandlerAsync<TQuery, TResult> _decorated;
        private readonly IPolicies _policies;

        /// <summary>
        /// Initializes a new instance of the <see cref="ARetryQueryHandlerDecoratorAsync{TQuery,TResult}" /> class.
        /// </summary>
        /// <param name="decorated">The decorated handler.</param>
        /// <param name="policies">The policies.</param>
        protected ARetryQueryHandlerDecoratorAsync(IQueryHandlerAsync<TQuery, TResult> decorated,
            IPolicies policies)
        {
            Guard.NotNull(decorated);
            Guard.NotNull(policies);

            _decorated = decorated;
            _policies = policies;
        }

        /// <summary>
        /// The name of the transport's retry-query pipeline in the policy registry.
        /// </summary>
        /// <remarks>
        /// The same pipeline the synchronous decorator uses; the asynchronous command decorators share
        /// their synchronous counterpart's name for the same reason.
        /// </remarks>
        protected abstract string PolicyName { get; }

        /// <inheritdoc />
        public async Task<TResult> HandleAsync(TQuery query)
        {
            Guard.NotNull(query);
            ResiliencePipeline pipeline = null;
            try
            {
                _policies.Registry.TryGetPipeline(PolicyName, out pipeline);
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
