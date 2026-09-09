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
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Validation;
using Polly;

namespace DotNetWorkQueue.Transport.SQLite.Decorator
{
    /// <inheritdoc />
    internal class BeginTransactionRetryDecorator : ISQLiteTransactionWrapper
    {
        private readonly ISQLiteTransactionWrapper _decorated;
        private readonly IPolicies _policies;
        private ResiliencePipeline _pipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="BeginTransactionRetryDecorator"/> class.
        /// </summary>
        /// <param name="decorated">The decorated.</param>
        /// <param name="policies">The policies.</param>
        public BeginTransactionRetryDecorator(ISQLiteTransactionWrapper decorated,
            IPolicies policies)
        {
            Guard.NotNull(decorated);
            Guard.NotNull(policies);

            _decorated = decorated;
            _policies = policies;
        }

        /// <inheritdoc />
        public DbConnection Connection
        {
            get => _decorated.Connection;
            set => _decorated.Connection = value;
        }

        /// <inheritdoc />
        public DbTransaction BeginTransaction()
        {
            var pipeline = Pipeline();
            if (pipeline == null) return _decorated.BeginTransaction();
            return pipeline.Execute(_ => _decorated.BeginTransaction());
        }

        /// <inheritdoc />
        public async Task<DbTransaction> BeginTransactionAsync()
        {
            var pipeline = Pipeline();
            if (pipeline == null) return await _decorated.BeginTransactionAsync().ConfigureAwait(false);
            return await pipeline.ExecuteAsync(async _ =>
                await _decorated.BeginTransactionAsync().ConfigureAwait(false)).ConfigureAwait(false);
        }

        private ResiliencePipeline Pipeline()
        {
            if (_pipeline != null) return _pipeline;
            try
            {
                _policies.Registry.TryGetPipeline(TransportPolicyDefinitions.BeginTransaction, out _pipeline);
            }
            catch (ObjectDisposedException)
            {
                // Shutdown race: registry disposed before first invocation.
                // Fall through to the direct handler - same semantics as the "no pipeline" path.
            }
            return _pipeline;
        }
    }
}
