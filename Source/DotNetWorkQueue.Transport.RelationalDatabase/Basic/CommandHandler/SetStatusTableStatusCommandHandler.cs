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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler
{
    /// <summary>
    /// Updates the status of a status record in the status table
    /// </summary>
    internal class SetStatusTableStatusCommandHandler : ICommandHandler<SetStatusTableStatusCommand>
    {
        private readonly CommandStringCache _commandCache;
        private readonly IDbConnectionFactory _dbConnectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetStatusTableStatusCommandHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        public SetStatusTableStatusCommandHandler(CommandStringCache commandCache,
                IDbConnectionFactory dbConnectionFactory)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);

            _commandCache = commandCache;
            _dbConnectionFactory = dbConnectionFactory;
        }

        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        public void Handle(SetStatusTableStatusCommand command)
        {
            using (var connection = _dbConnectionFactory.Create())
            {
                connection.Open();
                using (var commandSql = connection.CreateCommand())
                {
                    commandSql.CommandText = _commandCache.GetCommand(CommandStringTypes.UpdateStatusRecord);

                    var queueId = commandSql.CreateParameter();
                    queueId.ParameterName = "@QueueID";
                    queueId.DbType = DbType.Int64;
                    queueId.Value = command.QueueId;
                    commandSql.Parameters.Add(queueId);

                    var status = commandSql.CreateParameter();
                    status.ParameterName = "@Status";
                    status.DbType = DbType.Int16;
                    status.Value = Convert.ToInt16(command.Status);
                    commandSql.Parameters.Add(status);

                    commandSql.ExecuteNonQuery();
                }
            }
        }
    }
}
