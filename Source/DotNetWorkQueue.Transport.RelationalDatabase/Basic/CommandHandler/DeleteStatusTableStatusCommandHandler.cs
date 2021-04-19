// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler
{
    /// <inheritdoc />
    /// <summary>
    /// Deletes a record from the status table 
    /// </summary>
    internal class DeleteStatusTableStatusCommandHandler : ICommandHandler<DeleteStatusTableStatusCommand<long>>
    {
        private readonly IPrepareCommandHandler<DeleteStatusTableStatusCommand<long>> _prepareCommand;
        private readonly IDbConnectionFactory _dbConnectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteStatusTableStatusCommandHandler" /> class.
        /// </summary>
        /// <param name="prepareCommand">The command cache.</param>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        public DeleteStatusTableStatusCommandHandler(IPrepareCommandHandler<DeleteStatusTableStatusCommand<long>> prepareCommand, 
            IDbConnectionFactory dbConnectionFactory)
        {
            Guard.NotNull(() => prepareCommand, prepareCommand);
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);

            _prepareCommand = prepareCommand;
            _dbConnectionFactory = dbConnectionFactory;
        }

        /// <inheritdoc />
        public void Handle(DeleteStatusTableStatusCommand<long> command)
        {
            using (var connection = _dbConnectionFactory.Create())
            {
                connection.Open();
                using (var commandSql = connection.CreateCommand())
                {
                    _prepareCommand.Handle(command, commandSql, CommandStringTypes.DeleteFromStatus);
                    commandSql.ExecuteNonQuery();
                }
            }
        }
    }
}
