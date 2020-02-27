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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <inheritdoc />
    public class ResetHeartBeat : IResetHeartBeat
    {
        #region Member Level Variables
        private readonly QueueConsumerConfiguration _configuration;
        private readonly ICommandHandlerWithOutput<ResetHeartBeatCommand, long> _commandHandler;
        private readonly IQueryHandler<FindMessagesToResetByHeartBeatQuery, IEnumerable<MessageToReset>> _queryHandler;
        private readonly IGetTime _getTime;
        #endregion

        #region Constructor        
        /// <summary>
        /// Initializes a new instance of the <see cref="ResetHeartBeat"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="commandHandler">The command handler.</param>
        /// <param name="queryHandler">The query handler.</param>
        /// <param name="getTime">The get time.</param>
        public ResetHeartBeat(QueueConsumerConfiguration configuration, 
            ICommandHandlerWithOutput<ResetHeartBeatCommand, long> commandHandler, 
            IQueryHandler<FindMessagesToResetByHeartBeatQuery, IEnumerable<MessageToReset>> queryHandler,
            IGetTimeFactory getTime)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => commandHandler, commandHandler);
            Guard.NotNull(() => queryHandler, queryHandler);

            _configuration = configuration;
            _commandHandler = commandHandler;
            _queryHandler = queryHandler;
            _getTime = getTime.Create();
        }
        #endregion

        #region IResetHeartBeat

        /// <inheritdoc />
        public List<ResetHeartBeatOutput> Reset(CancellationToken cancelToken)
        {
            if (!_configuration.HeartBeat.Enabled)
                return new List<ResetHeartBeatOutput>(0);

            if (string.IsNullOrEmpty(_configuration.TransportConfiguration.ConnectionInfo.ConnectionString)) return new List<ResetHeartBeatOutput>(0);

            List<ResetHeartBeatOutput> returnData = new List<ResetHeartBeatOutput>();
            var query = new FindMessagesToResetByHeartBeatQuery(cancelToken);
            DateTime start = _getTime.GetCurrentUtcDate();
            var results = _queryHandler.Handle(query);
            DateTime end = _getTime.GetCurrentUtcDate();
            foreach (var result in results)
            {
                var id = new MessageQueueId(result.QueueId);
                var command = new ResetHeartBeatCommand(result);
                var resetResult = _commandHandler.Handle(command);
                if (resetResult <= 0) continue;
                var data = new ResetHeartBeatOutput(id, result.Headers, start, end);
                returnData.Add(data);
            }
            return returnData;
        }
        #endregion
    }
}
