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
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Shared.Basic
{
    /// <inheritdoc />
    public class ResetHeartBeat<T> : IResetHeartBeat
        where T : struct, IComparable<T>
    {
        #region Member Level Variables
        private readonly QueueConsumerConfiguration _configuration;
        private readonly ICommandHandlerWithOutput<ResetHeartBeatCommand<T>, long> _commandHandler;
        private readonly IQueryHandler<FindMessagesToResetByHeartBeatQuery<T>, IEnumerable<MessageToReset<T>>> _queryHandler;
        private readonly IGetTime _getTime;
        #endregion

        #region Constructor        
        /// <summary>
        /// Initializes a new instance of the <see cref="ResetHeartBeat{T}"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="commandHandler">The command handler.</param>
        /// <param name="queryHandler">The query handler.</param>
        /// <param name="getTime">The get time.</param>
        public ResetHeartBeat(QueueConsumerConfiguration configuration,
            ICommandHandlerWithOutput<ResetHeartBeatCommand<T>, long> commandHandler,
            IQueryHandler<FindMessagesToResetByHeartBeatQuery<T>, IEnumerable<MessageToReset<T>>> queryHandler,
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
            var query = new FindMessagesToResetByHeartBeatQuery<T>(cancelToken);
            DateTime start = _getTime.GetCurrentUtcDate();
            var results = _queryHandler.Handle(query);
            DateTime end = _getTime.GetCurrentUtcDate();
            foreach (var result in results)
            {
                var id = new MessageQueueId<T>(result.QueueId);
                var command = new ResetHeartBeatCommand<T>(result);
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
