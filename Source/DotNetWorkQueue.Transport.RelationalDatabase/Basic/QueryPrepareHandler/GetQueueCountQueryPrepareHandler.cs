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
using System;
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler
{
    internal class GetQueueCountQueryPrepareHandler : IPrepareQueryHandler<GetQueueCountQuery, long>
    {
        private readonly CommandStringCache _commandCache;
        private readonly Lazy<ITransportOptions> _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetErrorRetryCountQueryPrepareHandler{T}"/> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="options">The transport options.</param>
        public GetQueueCountQueryPrepareHandler(CommandStringCache commandCache, ITransportOptionsFactory options)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => options, options);

            _commandCache = commandCache;
            _options = new Lazy<ITransportOptions>(options.Create);
        }

        public void Handle(GetQueueCountQuery query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            if (query.Status.HasValue && _options.Value.EnableStatus) //status means nothing if not enabled on the queue
            {
                dbCommand.CommandText = _commandCache.GetCommand(CommandStringTypes.GetQueueCountStatus);
                var status = dbCommand.CreateParameter();
                status.ParameterName = "@Status";
                status.DbType = DbType.Int32;
                status.Value = query.Status.Value;
                dbCommand.Parameters.Add(status);
            }
            else
            {
                dbCommand.CommandText = _commandCache.GetCommand(CommandStringTypes.GetQueueCountAll);
            }
        }
    }
}
