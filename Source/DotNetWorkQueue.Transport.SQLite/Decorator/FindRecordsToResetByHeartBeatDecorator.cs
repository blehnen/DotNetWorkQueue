// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Decorator
{
    /// <summary>
    /// Handles various SQLite errors as warnings, not errors if they aren't critical
    /// </summary>
    internal class FindRecordsToResetByHeartBeatErrorDecorator : IQueryHandler<FindMessagesToResetByHeartBeatQuery, IEnumerable<MessageToReset>>
    {
        private readonly IQueryHandler<FindMessagesToResetByHeartBeatQuery, IEnumerable<MessageToReset>> _decorated;
        private readonly ILog _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindRecordsToResetByHeartBeatErrorDecorator"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="decorated">The decorated.</param>
        public FindRecordsToResetByHeartBeatErrorDecorator(ILogFactory logger,
            IQueryHandler<FindMessagesToResetByHeartBeatQuery, IEnumerable<MessageToReset>> decorated)
        {
            Guard.NotNull(() => decorated, decorated);
            Guard.NotNull(() => logger, logger);

            _logger = logger.Create("FindRecordsToResetByHeartBeat");
            _decorated = decorated;
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public IEnumerable<MessageToReset> Handle(FindMessagesToResetByHeartBeatQuery query)
        {
            try
            {
                return _decorated.Handle(query);
            }
            catch (Exception e)
            {
                if (e.Message.IndexOf("abort due to ROLLBACK", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    _logger.WarnException("The query has been aborted", e);
                    return Enumerable.Empty<MessageToReset>();
                }
                else
                    throw;
            }
        }
    }
}
