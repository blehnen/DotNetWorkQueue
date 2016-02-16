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
using System.Data.SQLite;
using DotNetWorkQueue.Transport.SQLite.Basic.Command;

namespace DotNetWorkQueue.Transport.SQLite.Basic.CommandHandler
{
    /// <summary>
    /// Sends a heart beat for a queue record
    /// </summary>
    internal class SendHeartBeatCommandHandler : ICommandHandlerWithOutput<SendHeartBeatCommand, DateTime?>
    {
        private readonly SqLiteCommandStringCache _commandCache;
        private readonly IConnectionInformation _connectionInformation;
        private readonly IGetTime _getTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendHeartBeatCommandHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="getTimeFactory">The get time factory.</param>
        public SendHeartBeatCommandHandler(SqLiteCommandStringCache commandCache, 
            IConnectionInformation connectionInformation,
            IGetTimeFactory getTimeFactory)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => connectionInformation, connectionInformation);

            _commandCache = commandCache;
            _connectionInformation = connectionInformation;
            _getTime = getTimeFactory.Create();
        }
        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        public DateTime? Handle(SendHeartBeatCommand command)
        {
            if (!DatabaseExists.Exists(_connectionInformation.ConnectionString))
            {
                return null;
            }

            using (var conn = new SQLiteConnection(_connectionInformation.ConnectionString))
            {
                conn.Open();
                using (var commandSql = conn.CreateCommand())
                {
                    commandSql.CommandText = _commandCache.GetCommand(SqLiteCommandStringTypes.SendHeartBeat);
                    commandSql.Parameters.Add("@QueueID", DbType.Int64);
                    commandSql.Parameters["@QueueID"].Value = command.QueueId;
                    commandSql.Parameters.Add("@status", DbType.Int32);
                    commandSql.Parameters["@status"].Value = Convert.ToInt16(QueueStatus.Processing);

                    commandSql.Parameters.Add("@date", DbType.Int64);
                    var date = _getTime.GetCurrentUtcDate();
                    commandSql.Parameters["@date"].Value = date.Ticks;

                    var results = commandSql.ExecuteNonQuery();
                    if (results != 1) return null; //return null if the record was not updated.
                    return date;
                }
            }
        }
    }
}
