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
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler
{
    /// <inheritdoc />
    /// <summary>
    /// Updates the error count for a record
    /// </summary>
    /// <remarks>
    /// One statement, always. It relies on the unique index on the error tracking table's
    /// (QueueID, ExceptionType), which every queue now has: new queues are created with it, and an
    /// older one gains it at schema version 1, which a producer or consumer refuses to start without
    /// (GitHub #308).
    ///
    /// There used to be a check-then-write fallback for queues lacking the index. It has been removed
    /// rather than left as a safety net, because it could not give the guarantee the index does: two
    /// failures of one message arriving together could each find no row and each insert one, and the
    /// count then read low, so the message got more attempts than it was configured for. Keeping a
    /// path that silently produces the wrong count is worse than failing on a schema that is no longer
    /// supported.
    /// </remarks>
    internal class SetErrorCountCommandHandler<T> : ICommandHandler<SetErrorCountCommand<T>>,
        ICommandHandlerAsync<SetErrorCountCommand<T>>
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IPrepareCommandHandler<SetErrorCountCommand<T>> _prepareCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetErrorCountCommandHandler{T}" /> class.
        /// </summary>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="prepareCommand">The prepare command.</param>
        public SetErrorCountCommandHandler(
            IDbConnectionFactory dbConnectionFactory,
            IPrepareCommandHandler<SetErrorCountCommand<T>> prepareCommand)
        {
            Guard.NotNull(dbConnectionFactory);
            Guard.NotNull(prepareCommand);

            _dbConnectionFactory = dbConnectionFactory;
            _prepareCommand = prepareCommand;
        }

        /// <inheritdoc />
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        public void Handle(SetErrorCountCommand<T> command)
        {
            using (var connection = _dbConnectionFactory.Create())
            {
                connection.Open();
                using (var commandSql = connection.CreateCommand())
                {
                    Prepare(command, commandSql);
                    commandSql.ExecuteNonQuery();
                }
            }
        }

        /// <inheritdoc />
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        public async Task HandleAsync(SetErrorCountCommand<T> command)
        {
            using (var connection = _dbConnectionFactory.Create())
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var commandSql = connection.CreateCommand())
                {
                    Prepare(command, commandSql);
                    await commandSql.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }

        private void Prepare(SetErrorCountCommand<T> command, DbCommand commandSql)
        {
            _prepareCommand.Handle(command, commandSql, CommandStringTypes.UpsertErrorCount);
        }
    }
}
