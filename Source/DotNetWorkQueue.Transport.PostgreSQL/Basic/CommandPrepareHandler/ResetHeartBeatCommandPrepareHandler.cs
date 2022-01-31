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
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandPrepareHandler
{
    /// <summary>
    /// 
    /// </summary>
    public class ResetHeartBeatCommandPrepareHandler : IPrepareCommandHandler<ResetHeartBeatCommand<long>>
    {
        private readonly CommandStringCache _commandCache;
        /// <summary>
        /// Initializes a new instance of the <see cref="ResetHeartBeatCommandPrepareHandler"/> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        public ResetHeartBeatCommandPrepareHandler(CommandStringCache commandCache)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
        }

        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="dbCommand">The database command.</param>
        /// <param name="commandType">Type of the command.</param>
        public void Handle(ResetHeartBeatCommand<long> command, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = _commandCache.GetCommand(CommandStringTypes.ResetHeartbeat);

            var param = dbCommand.CreateParameter();
            param.ParameterName = "@QueueID";
            param.DbType = DbType.Int64;
            param.Value = command.MessageReset.QueueId;
            dbCommand.Parameters.Add(param);

            param = dbCommand.CreateParameter();
            param.ParameterName = "@SourceStatus";
            param.DbType = DbType.Int32;
            param.Value = Convert.ToInt16(QueueStatuses.Processing);
            dbCommand.Parameters.Add(param);

            param = dbCommand.CreateParameter();
            param.ParameterName = "@Status";
            param.DbType = DbType.Int32;
            param.Value = Convert.ToInt16(QueueStatuses.Waiting);
            dbCommand.Parameters.Add(param);

            param = dbCommand.CreateParameter();
            param.ParameterName = "@HeartBeat";
            param.DbType = DbType.Int64;
            param.Value = command.MessageReset.HeartBeat.Ticks;
            dbCommand.Parameters.Add(param);
        }
    }
}
