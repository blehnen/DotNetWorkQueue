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
using System.Data;
using System.Data.SqlClient;
using DotNetWorkQueue.Transport.SqlServer.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler
{
    /// <summary>
    /// 
    /// </summary>
    public class SetJobLastKnownEventCommandHandler : ICommandHandler<SetJobLastKnownEventCommand>
    {
        private readonly SqlServerCommandStringCache _commandCache;
        private readonly IConnectionInformation _connectionInformation;
        /// <summary>
        /// Initializes a new instance of the <see cref="SetJobLastKnownEventCommandHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public SetJobLastKnownEventCommandHandler(SqlServerCommandStringCache commandCache,
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
        public void Handle(SetJobLastKnownEventCommand command)
        {
            using (var conn = new SqlConnection(_connectionInformation.ConnectionString))
            {
                conn.Open();
                using (var commandSql = conn.CreateCommand())
                {
                    commandSql.CommandText = _commandCache.GetCommand(SqlServerCommandStringTypes.SetJobLastKnownEvent);
                    commandSql.Parameters.Add("@JobName", SqlDbType.VarChar);
                    commandSql.Parameters["@JobName"].Value = command.JobName;
                    commandSql.Parameters.Add("@JobEventTime", SqlDbType.DateTimeOffset);
                    commandSql.Parameters["@JobEventTime"].Value = command.JobEventTime;
                    commandSql.Parameters.Add("@JobScheduledTime", SqlDbType.DateTimeOffset);
                    commandSql.Parameters["@JobScheduledTime"].Value = command.JobScheduledTime;
                    commandSql.ExecuteNonQuery();
                }
            }
        }
    }
}
