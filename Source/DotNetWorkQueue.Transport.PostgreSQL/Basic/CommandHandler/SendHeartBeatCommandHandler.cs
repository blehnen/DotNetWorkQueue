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
using DotNetWorkQueue.Transport.PostgreSQL.Basic.Command;
using DotNetWorkQueue.Validation;
using Npgsql;
using NpgsqlTypes;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandHandler
{
    /// <summary>
    /// Sends a heart beat for a queue record
    /// </summary>
    internal class SendHeartBeatCommandHandler : ICommandHandlerWithOutput<SendHeartBeatCommand, DateTime?>
    {
        private readonly PostgreSqlCommandStringCache _commandCache;
        private readonly IConnectionInformation _connectionInformation;
        private readonly IGetTime _getTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendHeartBeatCommandHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="getTimeFactory">The get time factory.</param>
        public SendHeartBeatCommandHandler(PostgreSqlCommandStringCache commandCache, 
            IConnectionInformation connectionInformation,
            IGetTimeFactory getTimeFactory)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => getTimeFactory, getTimeFactory);

            _commandCache = commandCache;
            _connectionInformation = connectionInformation;
            _getTime = getTimeFactory.Create();
        }
        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        public DateTime? Handle(SendHeartBeatCommand command)
        {
            using (var conn = new NpgsqlConnection(_connectionInformation.ConnectionString))
            {
                conn.Open();
                using (var commandSql = conn.CreateCommand())
                {
                    commandSql.CommandText = _commandCache.GetCommand(PostgreSqlCommandStringTypes.SendHeartBeat);
                    commandSql.Parameters.Add("@QueueID", NpgsqlDbType.Bigint);
                    commandSql.Parameters["@QueueID"].Value = command.QueueId;
                    commandSql.Parameters.Add("@status", NpgsqlDbType.Integer);
                    commandSql.Parameters["@status"].Value = Convert.ToInt16(QueueStatuses.Processing);
                    var date = _getTime.GetCurrentUtcDate();
                    commandSql.Parameters.Add("@date", NpgsqlDbType.Bigint);
                    commandSql.Parameters["@date"].Value = date.Ticks;
                    var records = commandSql.ExecuteNonQuery();
                    if (records != 1) return null;
                    return date;
                }
            }
        }
    }
}
