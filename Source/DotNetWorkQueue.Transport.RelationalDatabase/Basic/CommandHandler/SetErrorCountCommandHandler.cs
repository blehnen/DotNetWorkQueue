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

using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler
{
    /// <inheritdoc />
    /// <summary>
    /// Updates the error count for a record
    /// </summary>
    internal class SetErrorCountCommandHandler : ICommandHandler<SetErrorCountCommand>
    {
        private readonly IQueryHandler<GetErrorRecordExistsQuery, bool> _queryHandler;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IPrepareCommandHandler<SetErrorCountCommand> _prepareCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetErrorCountCommandHandler" /> class.
        /// </summary>
        /// <param name="queryHandler">The query handler.</param>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="prepareCommand">The prepare command.</param>
        public SetErrorCountCommandHandler(
            IQueryHandler<GetErrorRecordExistsQuery, bool> queryHandler,
            IDbConnectionFactory dbConnectionFactory,
            IPrepareCommandHandler<SetErrorCountCommand> prepareCommand)
        {
            Guard.NotNull(() => queryHandler, queryHandler);
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            Guard.NotNull(() => prepareCommand, prepareCommand);

            _queryHandler = queryHandler;
            _dbConnectionFactory = dbConnectionFactory;
            _prepareCommand = prepareCommand;
        }
        /// <inheritdoc />
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        public void Handle(SetErrorCountCommand command)
        {
            using (var connection = _dbConnectionFactory.Create())
            {
                connection.Open();
                using (var commandSql = connection.CreateCommand())
                {
                    var commandType = _queryHandler.Handle(new GetErrorRecordExistsQuery(command.ExceptionType,
                        command.QueueId))
                        ? CommandStringTypes.UpdateErrorCount
                        : CommandStringTypes.InsertErrorCount;
                    _prepareCommand.Handle(command, commandSql, commandType);
                    commandSql.ExecuteNonQuery();
                }
            }
        }
    }
}
