// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandHandler
{
    /// <inheritdoc />
    public class SetJobLastKnownEventCommandHandler : ICommandHandler<SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction>>
    {
        private readonly PostgreSqlCommandStringCache _commandCache;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        /// <summary>
        /// Initializes a new instance of the <see cref="SetJobLastKnownEventCommandHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        public SetJobLastKnownEventCommandHandler(PostgreSqlCommandStringCache commandCache,
            IDbConnectionFactory dbConnectionFactory)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);

            _commandCache = commandCache;
            _dbConnectionFactory = dbConnectionFactory;
        }
        /// <inheritdoc />
        public void Handle(SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction> command)
        {
            using (var conn = _dbConnectionFactory.Create())
            {
                conn.Open();
                using (var commandSql = conn.CreateCommand())
                {
                    commandSql.CommandText = _commandCache.GetCommand(CommandStringTypes.SetJobLastKnownEvent);

                    var jobNameParam = commandSql.CreateParameter();
                    jobNameParam.ParameterName = "@JobName";
                    jobNameParam.DbType = DbType.AnsiString;
                    jobNameParam.Value = command.JobName;
                    commandSql.Parameters.Add(jobNameParam);

                    var jobEventTimeParam = commandSql.CreateParameter();
                    jobEventTimeParam.ParameterName = "@JobEventTime";
                    jobEventTimeParam.DbType = DbType.Int64;
                    jobEventTimeParam.Value = command.JobEventTime.UtcDateTime.Ticks;
                    commandSql.Parameters.Add(jobEventTimeParam);

                    var jobScheduledTimeParam = commandSql.CreateParameter();
                    jobScheduledTimeParam.ParameterName = "@JobScheduledTime";
                    jobScheduledTimeParam.DbType = DbType.Int64;
                    jobScheduledTimeParam.Value = command.JobScheduledTime.UtcDateTime.Ticks;
                    commandSql.Parameters.Add(jobScheduledTimeParam);

                    commandSql.ExecuteNonQuery();
                }
            }
        }
    }
}
