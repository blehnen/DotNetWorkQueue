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
    /// Sends a heart beat for a queue record
    /// </summary>
    internal class SendHeartBeatCommandHandler : ICommandHandlerWithOutput<SendHeartBeatCommand, DateTime?>
    {
        private readonly SqlServerCommandStringCache _commandCache;
        private readonly IConnectionInformation _connectionInformation;
        /// <summary>
        /// Initializes a new instance of the <see cref="SendHeartBeatCommandHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public SendHeartBeatCommandHandler(SqlServerCommandStringCache commandCache, 
            IConnectionInformation connectionInformation)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => connectionInformation, connectionInformation);

            _commandCache = commandCache;
            _connectionInformation = connectionInformation;
        }
        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        public DateTime? Handle(SendHeartBeatCommand command)
        {
            using (var conn = new SqlConnection(_connectionInformation.ConnectionString))
            {
                conn.Open();
                using (var commandSql = conn.CreateCommand())
                {
                    commandSql.CommandText = _commandCache.GetCommand(SqlServerCommandStringTypes.SendHeartBeat);
                    commandSql.Parameters.Add("@QueueID", SqlDbType.BigInt);
                    commandSql.Parameters["@QueueID"].Value = command.QueueId;
                    commandSql.Parameters.Add("@status", SqlDbType.Int);
                    commandSql.Parameters["@status"].Value = Convert.ToInt16(QueueStatus.Processing);
                    using (var reader = commandSql.ExecuteReader())
                    {
                        if (reader.RecordsAffected != 1) return null; //return null if the record was not updated.
                        if (reader.Read())
                        {
                            return reader.GetDateTime(0);
                        }
                    }
                    return null; //return null if the record was not updated.
                }
            }
        }
    }
}
