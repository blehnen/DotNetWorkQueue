﻿// ---------------------------------------------------------------------
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
using System.Collections.Generic;
using System.Linq;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Decorator
{
    /// <summary>
    /// 
    /// </summary>
    public class FindRecordsToResetByHeartBeatDecorator : IQueryHandler<FindMessagesToResetByHeartBeatQuery<long>, IEnumerable<MessageToReset<long>>>
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly IQueryHandler<FindMessagesToResetByHeartBeatQuery<long>, IEnumerable<MessageToReset<long>>> _decorated;
        private readonly DatabaseExists _databaseExists;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessageCommandDecorator" /> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="decorated">The decorated.</param>
        /// <param name="databaseExists">The database exists.</param>
        public FindRecordsToResetByHeartBeatDecorator(IConnectionInformation connectionInformation,
            IQueryHandler<FindMessagesToResetByHeartBeatQuery<long>, IEnumerable<MessageToReset<long>>> decorated,
            DatabaseExists databaseExists)
        {
            Guard.NotNull(() => decorated, decorated);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => databaseExists, databaseExists);
            _connectionInformation = connectionInformation;
            _decorated = decorated;
            _databaseExists = databaseExists;
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public IEnumerable<MessageToReset<long>> Handle(FindMessagesToResetByHeartBeatQuery<long> query)
        {
            return !_databaseExists.Exists(_connectionInformation.ConnectionString) ? Enumerable.Empty<MessageToReset<long>>() : _decorated.Handle(query);
        }
    }
}
