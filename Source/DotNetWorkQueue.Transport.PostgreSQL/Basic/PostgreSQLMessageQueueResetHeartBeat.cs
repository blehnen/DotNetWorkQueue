// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.PostgreSQL.Basic.Command;
using DotNetWorkQueue.Transport.PostgreSQL.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <summary>
    /// Searches for and updates work items that are outside of the heart beat window
    /// </summary>
    internal class PostgreSqlMessageQueueResetHeartBeat : IResetHeartBeat
    {
        #region Member Level Variables
        private readonly QueueConsumerConfiguration _configuration;
        private readonly ICommandHandlerWithOutput<ResetHeartBeatCommand, long> _commandHandler;
        private readonly IQueryHandler<FindMessagesToResetByHeartBeatQuery, IEnumerable<MessageToReset>> _queryHandler;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlMessageQueueResetHeartBeat"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="commandHandler">The command handler.</param>
        /// <param name="queryHandler">The query handler.</param>
        public PostgreSqlMessageQueueResetHeartBeat(QueueConsumerConfiguration configuration, 
            ICommandHandlerWithOutput<ResetHeartBeatCommand, long> commandHandler, 
            IQueryHandler<FindMessagesToResetByHeartBeatQuery, IEnumerable<MessageToReset>> queryHandler)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => commandHandler, commandHandler);
            Guard.NotNull(() => queryHandler, queryHandler);

            _configuration = configuration;
            _commandHandler = commandHandler;
            _queryHandler = queryHandler;
        }
        #endregion

        #region IResetHeartBeat

        /// <summary>
        /// Used to find and reset work items that are out of the heart beat window
        /// </summary>
        public long Reset(CancellationToken cancelToken)
        {
            if (!_configuration.HeartBeat.Enabled)
                return 0;

            if (string.IsNullOrEmpty(_configuration.TransportConfiguration.ConnectionInfo.ConnectionString)) return 0;

            var query = new FindMessagesToResetByHeartBeatQuery(cancelToken);
            return _queryHandler.Handle(query).Select(queueid => 
                new ResetHeartBeatCommand(queueid)).Aggregate<ResetHeartBeatCommand, long>(0, (current, command) => 
                    current + _commandHandler.Handle(command));
        }
        #endregion
    }
}
