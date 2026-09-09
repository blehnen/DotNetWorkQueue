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
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Validation;
using Polly;

namespace DotNetWorkQueue.Transport.SQLite.Decorator
{
    /// <inheritdoc />
    internal class RetryCommandHandlerDecoratorAsync<TCommand> : ICommandHandlerAsync<TCommand>
    {
        private readonly ICommandHandlerAsync<TCommand> _decorated;
        private readonly IPolicies _policies;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryCommandHandlerDecoratorAsync{TCommand}" /> class.
        /// </summary>
        /// <param name="decorated">The decorated handler.</param>
        /// <param name="policies">The policies.</param>
        public RetryCommandHandlerDecoratorAsync(ICommandHandlerAsync<TCommand> decorated,
            IPolicies policies)
        {
            Guard.NotNull(decorated);
            Guard.NotNull(policies);

            _decorated = decorated;
            _policies = policies;
        }

        /// <inheritdoc />
        public async Task HandleAsync(TCommand command)
        {
            //IRetrySkippable is deliberately not honoured here, matching the synchronous no-output
            //decorator. Only the two send commands implement it, and those run through the
            //with-output handler - so a check here would be dead code today, and adding it to one
            //side would make the same command retry differently depending on which path called it.
            ResiliencePipeline pipeline = null;
            try
            {
                _policies.Registry.TryGetPipeline(TransportPolicyDefinitions.RetryCommandHandlerAsync, out pipeline);
            }
            catch (ObjectDisposedException)
            {
                // Shutdown race: registry disposed before the last handler call.
                // Fall through to direct handler — same semantics as the "no policy" branch.
            }

            if (pipeline != null)
                await pipeline.ExecuteAsync(async _ => await _decorated.HandleAsync(command).ConfigureAwait(false)).ConfigureAwait(false);
            else
                await _decorated.HandleAsync(command).ConfigureAwait(false);
        }
    }
}