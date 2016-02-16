// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using System.Data.SqlClient;
using DotNetWorkQueue.Transport.SqlServer.Basic.Command;
namespace DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler
{
    /// <summary>
    /// Resets the status for a specific record
    /// </summary>
    internal class ResetHeartBeatCommandHandler : ICommandHandlerWithOutput<ResetHeartBeatCommand, long>
    {
        private readonly SqlServerCommandStringCache _commandCache;
        private readonly IConnectionInformation _connectionInformation;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResetHeartBeatCommandHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public ResetHeartBeatCommandHandler(SqlServerCommandStringCache commandCache, 
            IConnectionInformation connectionInformation)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => connectionInformation, connectionInformation);

            _commandCache = commandCache;
            _connectionInformation = connectionInformation;
        }
        /// <summary>
        /// Resets the status for a specific record, if the status is currently 1
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        public long Handle(ResetHeartBeatCommand command)
        {
            using (var connection = new SqlConnection(_connectionInformation.ConnectionString))
            {
                connection.Open();
                using (var sqlCommand = connection.CreateCommand())
                {
                    sqlCommand.CommandText = _commandCache.GetCommand(SqlServerCommandStringTypes.ResetHeartbeat);
                    sqlCommand.Parameters.Add("@QueueID", SqlDbType.BigInt);
                    sqlCommand.Parameters.Add("@SourceStatus", SqlDbType.Int);
                    sqlCommand.Parameters.Add("@Status", SqlDbType.Int);
                    sqlCommand.Parameters.Add("@HeartBeat", SqlDbType.DateTime);
                    sqlCommand.Parameters["@QueueID"].Value = command.MessageReset.QueueId;
                    sqlCommand.Parameters["@Status"].Value = Convert.ToInt16(QueueStatus.Waiting);
                    sqlCommand.Parameters["@SourceStatus"].Value = Convert.ToInt16(QueueStatus.Processing);
                    sqlCommand.Parameters["@HeartBeat"].Value = command.MessageReset.HeartBeat;
                    return sqlCommand.ExecuteNonQuery();
                }
            }
        }
    }
}
