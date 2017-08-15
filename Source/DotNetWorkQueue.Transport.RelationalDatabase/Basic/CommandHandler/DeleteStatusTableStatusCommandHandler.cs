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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler
{
    /// <summary>
    /// Deletes a record from the status table 
    /// </summary>
    internal class DeleteStatusTableStatusCommandHandler : ICommandHandler<DeleteStatusTableStatusCommand>
    {
        private readonly CommandStringCache _commandCache;
        private readonly IDbConnectionFactory _dbConnectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteStatusTableStatusCommandHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        public DeleteStatusTableStatusCommandHandler(CommandStringCache commandCache, 
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
        public void Handle(DeleteStatusTableStatusCommand command)
        {
            using (var connection = _dbConnectionFactory.Create())
            {
                connection.Open();
                using (var commandSql = connection.CreateCommand())
                {
                    commandSql.CommandText = _commandCache.GetCommand(CommandStringTypes.DeleteFromStatus);
                    var param = commandSql.CreateParameter();
                    param.ParameterName = "@QueueID";
                    param.Value = command.QueueId;
                    param.DbType = DbType.Int64;
                    commandSql.ExecuteNonQuery();
                }
            }
        }
    }
}
