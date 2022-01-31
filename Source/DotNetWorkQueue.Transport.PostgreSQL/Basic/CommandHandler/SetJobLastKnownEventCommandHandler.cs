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
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;
using Npgsql;
using NpgsqlTypes;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandHandler
{
    /// <inheritdoc />
    public class SetJobLastKnownEventCommandHandler : ICommandHandler<SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction>>
    {
        private readonly PostgreSqlCommandStringCache _commandCache;
        private readonly IConnectionInformation _connectionInformation;
        /// <summary>
        /// Initializes a new instance of the <see cref="SetJobLastKnownEventCommandHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public SetJobLastKnownEventCommandHandler(PostgreSqlCommandStringCache commandCache,
            IConnectionInformation connectionInformation)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => connectionInformation, connectionInformation);

            _commandCache = commandCache;
            _connectionInformation = connectionInformation;
        }
        /// <inheritdoc />
        public void Handle(SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction> command)
        {
            using (var conn = new NpgsqlConnection(_connectionInformation.ConnectionString))
            {
                conn.Open();
                using (var commandSql = conn.CreateCommand())
                {
                    commandSql.CommandText = _commandCache.GetCommand(CommandStringTypes.SetJobLastKnownEvent);
                    commandSql.Parameters.Add("@JobName", NpgsqlDbType.Varchar);
                    commandSql.Parameters["@JobName"].Value = command.JobName;
                    commandSql.Parameters.Add("@JobEventTime", NpgsqlDbType.Bigint);
                    commandSql.Parameters["@JobEventTime"].Value = command.JobEventTime.UtcDateTime.Ticks;
                    commandSql.Parameters.Add("@JobScheduledTime", NpgsqlDbType.Bigint);
                    commandSql.Parameters["@JobScheduledTime"].Value = command.JobScheduledTime.UtcDateTime.Ticks;
                    commandSql.ExecuteNonQuery();
                }
            }
        }
    }
}
