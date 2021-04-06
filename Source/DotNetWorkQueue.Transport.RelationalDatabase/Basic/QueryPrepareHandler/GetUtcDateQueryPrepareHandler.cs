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
using System;
using System.Data;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler
{
    /// <summary>
    /// Prepares to run the get utc date query
    /// </summary>
    /// <inheritdoc />
    public class GetUtcDateQueryPrepareHandler : IPrepareQueryHandler<GetUtcDateQuery, DateTime>
    {
        private readonly CommandStringCache _commandCache;
        /// <summary>
        /// Initializes a new instance of the <see cref="GetUtcDateQueryPrepareHandler"/> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        public GetUtcDateQueryPrepareHandler(CommandStringCache commandCache)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
        }
        /// <inheritdoc />
        public void Handle(GetUtcDateQuery query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = _commandCache.GetCommand(commandType);
        }
    }
}
