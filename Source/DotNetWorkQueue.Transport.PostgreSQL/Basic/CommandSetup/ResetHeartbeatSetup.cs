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
using System;
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using Npgsql;
using NpgsqlTypes;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandSetup
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Transport.RelationalDatabase.ISetupCommand" />
    public class ResetHeartbeatSetup : ISetupCommand
    {
        /// <summary>
        /// Setup the specified input command.
        /// </summary>
        /// <param name="inputCommand">The input command.</param>
        /// <param name="type">The type.</param>
        /// <param name="commandParams">The command parameters.</param>
        public void Setup(IDbCommand inputCommand, CommandStringTypes type, object commandParams)
        {
            var npgsqlCommand = (NpgsqlCommand) inputCommand;
            var commandInput = (ResetHeartBeatCommand)commandParams;

            npgsqlCommand.Parameters.Add("@QueueID", NpgsqlDbType.Bigint);
            npgsqlCommand.Parameters.Add("@SourceStatus", NpgsqlDbType.Integer);
            npgsqlCommand.Parameters.Add("@Status", NpgsqlDbType.Integer);
            npgsqlCommand.Parameters.Add("@HeartBeat", NpgsqlDbType.Bigint);
            npgsqlCommand.Parameters["@QueueID"].Value = commandInput.MessageReset.QueueId;
            npgsqlCommand.Parameters["@Status"].Value = Convert.ToInt16(QueueStatuses.Waiting);
            npgsqlCommand.Parameters["@SourceStatus"].Value = Convert.ToInt16(QueueStatuses.Processing);
            npgsqlCommand.Parameters["@HeartBeat"].Value = commandInput.MessageReset.HeartBeat.Ticks;
        }
    }
}
