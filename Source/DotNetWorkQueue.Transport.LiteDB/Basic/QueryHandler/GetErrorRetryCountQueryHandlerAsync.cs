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
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.QueryHandler
{
    /// <summary>
    /// Returns the current retry count for a message and a specific exception type
    /// </summary>
    /// <remarks>
    /// LiteDB has no asynchronous API, so this runs the synchronous handler on the thread pool.
    /// Issue #283 tracks replacing this once LiteDB v6 ships real async methods.
    /// </remarks>
    public class GetErrorRetryCountQueryHandlerAsync : IQueryHandlerAsync<GetErrorRetryCountQuery<int>, int>
    {
        private readonly IQueryHandler<GetErrorRetryCountQuery<int>, int> _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetErrorRetryCountQueryHandlerAsync"/> class.
        /// </summary>
        /// <param name="handler">The synchronous handler that does the work.</param>
        public GetErrorRetryCountQueryHandlerAsync(IQueryHandler<GetErrorRetryCountQuery<int>, int> handler)
        {
            Guard.NotNull(handler);
            _handler = handler;
        }

        /// <inheritdoc />
        public async Task<int> HandleAsync(GetErrorRetryCountQuery<int> query)
        {
            return await Task.Run(() => _handler.Handle(query)).ConfigureAwait(false);
        }
    }
}
