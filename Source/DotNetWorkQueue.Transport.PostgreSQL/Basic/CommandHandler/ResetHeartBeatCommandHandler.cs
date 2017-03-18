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
    /// Resets the status for a specific record
    /// </summary>
    internal class ResetHeartBeatCommandHandler : ICommandHandlerWithOutput<ResetHeartBeatCommand, long>
    {
        private readonly PostgreSqlCommandStringCache _commandCache;
        private readonly IConnectionInformation _connectionInformation;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResetHeartBeatCommandHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public ResetHeartBeatCommandHandler(PostgreSqlCommandStringCache commandCache, 
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
            using (var connection = new NpgsqlConnection(_connectionInformation.ConnectionString))
            {
                connection.Open();
                using (var npgsqlCommand = connection.CreateCommand())
                {
                    npgsqlCommand.CommandText = _commandCache.GetCommand(PostgreSqlCommandStringTypes.ResetHeartbeat);
                    npgsqlCommand.Parameters.Add("@QueueID", NpgsqlDbType.Bigint);
                    npgsqlCommand.Parameters.Add("@SourceStatus", NpgsqlDbType.Integer);
                    npgsqlCommand.Parameters.Add("@Status", NpgsqlDbType.Integer);
                    npgsqlCommand.Parameters.Add("@HeartBeat", NpgsqlDbType.Bigint);
                    npgsqlCommand.Parameters["@QueueID"].Value = command.MessageReset.QueueId;
                    npgsqlCommand.Parameters["@Status"].Value = Convert.ToInt16(QueueStatuses.Waiting);
                    npgsqlCommand.Parameters["@SourceStatus"].Value = Convert.ToInt16(QueueStatuses.Processing);
                    npgsqlCommand.Parameters["@HeartBeat"].Value = command.MessageReset.HeartBeat.Ticks;
                    return npgsqlCommand.ExecuteNonQuery();
                }
            }
        }
    }
}
