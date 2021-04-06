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
using System.Collections.Generic;
using System.Linq;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Query;

namespace DotNetWorkQueue.Transport.SQLite.Decorator
{
    internal class FindExpiredRecordsToDeleteQueryHandlerErrorDecorator : IQueryHandler<FindExpiredMessagesToDeleteQuery<long>, IEnumerable<long>>
    {
        private readonly IQueryHandler<FindExpiredMessagesToDeleteQuery<long>, IEnumerable<long>> _decorated;
        private readonly ILogger _logger;
        public FindExpiredRecordsToDeleteQueryHandlerErrorDecorator(
            IQueryHandler<FindExpiredMessagesToDeleteQuery<long>, IEnumerable<long>> decorated,
            ILogger logger)
        {
            _decorated = decorated;
            _logger = logger;
        }

        public IEnumerable<long> Handle(FindExpiredMessagesToDeleteQuery<long> query)
        {
            try
            {
                return _decorated.Handle(query);
            }
            catch (Exception e)
            {
                if (e.Message.IndexOf("abort due to ROLLBACK", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    _logger.LogWarning("The query has been aborted", e);
                    return Enumerable.Empty<long>();
                }
                else
                    throw;
            }
        }
    }
}
