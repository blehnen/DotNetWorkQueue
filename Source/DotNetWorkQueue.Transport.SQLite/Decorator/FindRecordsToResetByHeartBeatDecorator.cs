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
using System.Collections.Generic;
using System.Linq;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Transport.SQLite.Decorator
{
    /// <summary>
    /// Handles various SQLite errors as warnings, not errors if they aren't critical
    /// </summary>
    internal class FindRecordsToResetByHeartBeatErrorDecorator : IQueryHandler<FindMessagesToResetByHeartBeatQuery<long>, IEnumerable<MessageToReset<long>>>
    {
        private readonly IQueryHandler<FindMessagesToResetByHeartBeatQuery<long>, IEnumerable<MessageToReset<long>>> _decorated;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindRecordsToResetByHeartBeatErrorDecorator"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="decorated">The decorated.</param>
        public FindRecordsToResetByHeartBeatErrorDecorator(ILogger logger,
            IQueryHandler<FindMessagesToResetByHeartBeatQuery<long>, IEnumerable<MessageToReset<long>>> decorated)
        {
            Guard.NotNull(() => decorated, decorated);
            Guard.NotNull(() => logger, logger);

            _logger = logger;
            _decorated = decorated;
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public IEnumerable<MessageToReset<long>> Handle(FindMessagesToResetByHeartBeatQuery<long> query)
        {
            try
            {
                return _decorated.Handle(query);
            }
            catch (Exception e)
            {
                if (e.Message.IndexOf("abort due to ROLLBACK", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    _logger.LogWarning($"The query has been aborted{System.Environment.NewLine}{e}");
                    return Enumerable.Empty<MessageToReset<long>>();
                }
                else
                    throw;
            }
        }
    }
}
