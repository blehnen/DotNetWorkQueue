// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandPrepareHandler
{
    /// <inheritdoc />
    public class SendHeartBeatCommandPrepareHandler: IPrepareCommandHandlerWithOutput<SendHeartBeatCommand<long>, DateTime>
    {
        private readonly CommandStringCache _commandCache;
        private readonly IGetTime _getTime;
        /// <summary>
        /// Initializes a new instance of the <see cref="SendHeartBeatCommandPrepareHandler"/> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="getTimeFactory">The get time factory.</param>
        public SendHeartBeatCommandPrepareHandler(CommandStringCache commandCache,
            IGetTimeFactory getTimeFactory)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => getTimeFactory, getTimeFactory);
            _commandCache = commandCache;
            _getTime = getTimeFactory.Create();
        }
        /// <inheritdoc />
        public DateTime Handle(SendHeartBeatCommand<long> command, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = _commandCache.GetCommand(commandType);

            var param = dbCommand.CreateParameter();
            param.ParameterName = "@QueueID";
            param.Value = command.QueueId;
            param.DbType = DbType.Int64;
            dbCommand.Parameters.Add(param);

            param = dbCommand.CreateParameter();
            param.ParameterName = "@Status";
            param.DbType = DbType.Int32;
            param.Value = Convert.ToInt16(QueueStatuses.Processing);
            dbCommand.Parameters.Add(param);

            param = dbCommand.CreateParameter();
            param.ParameterName = "@date";
            param.DbType = DbType.Int64;
            var date = _getTime.GetCurrentUtcDate();
            param.Value = date.Ticks;
            dbCommand.Parameters.Add(param);

            return date;
        }
    }
}
