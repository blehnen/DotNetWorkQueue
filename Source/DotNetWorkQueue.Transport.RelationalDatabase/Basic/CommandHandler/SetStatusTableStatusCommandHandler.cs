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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler
{
    /// <summary>
    /// Updates the status of a status record in the status table
    /// </summary>
    internal class SetStatusTableStatusCommandHandler : ICommandHandler<SetStatusTableStatusCommand>
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IPrepareCommandHandler<SetStatusTableStatusCommand> _prepareCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetStatusTableStatusCommandHandler" /> class.
        /// </summary>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="prepareCommand">The prepare command.</param>
        public SetStatusTableStatusCommandHandler(IDbConnectionFactory dbConnectionFactory,
                IPrepareCommandHandler<SetStatusTableStatusCommand> prepareCommand)
        {
            Guard.NotNull(() => prepareCommand, prepareCommand);
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);

            _dbConnectionFactory = dbConnectionFactory;
            _prepareCommand = prepareCommand;
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
                    _prepareCommand.Handle(command, commandSql, CommandStringTypes.UpdateStatusRecord);
                    commandSql.ExecuteNonQuery();
                }
            }
        }
    }
}
