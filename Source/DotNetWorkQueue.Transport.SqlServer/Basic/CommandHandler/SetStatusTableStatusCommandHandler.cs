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
using System.Data;
using System.Data.SqlClient;
using DotNetWorkQueue.Transport.SqlServer.Basic.Command;
namespace DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler
{
    /// <summary>
    /// Updates the status of a status record in the status table
    /// </summary>
    internal class SetStatusTableStatusCommandHandler : ICommandHandler<SetStatusTableStatusCommand>
    {
        private readonly SqlServerCommandStringCache _commandCache;
        private readonly IConnectionInformation _connectionInformation;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetStatusTableStatusCommandHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public SetStatusTableStatusCommandHandler(SqlServerCommandStringCache commandCache,
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
        public void Handle(SetStatusTableStatusCommand command)
        {
            using (var connection = new SqlConnection(_connectionInformation.ConnectionString))
            {
                connection.Open();
                using (var commandSql = connection.CreateCommand())
                {
                    commandSql.CommandText = _commandCache.GetCommand(SqlServerCommandStringTypes.UpdateStatusRecord);

                    commandSql.Parameters.Add("@QueueID", SqlDbType.BigInt);
                    commandSql.Parameters["@QueueID"].Value = command.QueueId;

                    commandSql.Parameters.Add("@Status", SqlDbType.TinyInt);
                    commandSql.Parameters["@Status"].Value = command.Status;

                    commandSql.ExecuteNonQuery();
                }
            }
        }
    }
}
