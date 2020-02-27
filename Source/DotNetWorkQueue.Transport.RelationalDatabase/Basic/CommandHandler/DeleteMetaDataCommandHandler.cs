// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler
{
    /// <inheritdoc />
    /// <summary>
    /// Deletes the meta data for a message
    /// </summary>
    public class DeleteMetaDataCommandHandler : ICommandHandler<DeleteMetaDataCommand>
    {
        private readonly IPrepareCommandHandler<DeleteMetaDataCommand> _prepareCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMetaDataCommandHandler" /> class.
        /// </summary>
        /// <param name="prepareCommand">The prepare command.</param>
        public DeleteMetaDataCommandHandler(
            IPrepareCommandHandler<DeleteMetaDataCommand> prepareCommand)
        {
            Guard.NotNull(() => prepareCommand, prepareCommand);

            _prepareCommand = prepareCommand;
        }

        /// <inheritdoc />
        public void Handle(DeleteMetaDataCommand command)
        {
            using (
                var commandSqlDeleteMetaData = command.Connection.CreateCommand())
            {
                _prepareCommand.Handle(command, commandSqlDeleteMetaData, CommandStringTypes.DeleteFromMetaData);
                commandSqlDeleteMetaData.Transaction = command.Transaction;
                commandSqlDeleteMetaData.ExecuteNonQuery();
            }
        }
    }
}
