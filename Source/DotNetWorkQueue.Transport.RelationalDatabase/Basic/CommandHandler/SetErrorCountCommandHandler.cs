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
using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler
{
    /// <inheritdoc />
    /// <summary>
    /// Updates the error count for a record
    /// </summary>
    internal class SetErrorCountCommandHandler<T> : ICommandHandler<SetErrorCountCommand<T>>,
        ICommandHandlerAsync<SetErrorCountCommand<T>>
    {
        private readonly IQueryHandler<GetErrorRecordExistsQuery<T>, bool> _queryHandler;
        //Asked once per queue: the schema does not change underneath a running consumer, and the answer
        //decides whether the atomic statement is available at all.
        private readonly Lazy<bool> _canUpsert;
        private readonly IQueryHandlerAsync<GetErrorRecordExistsQuery<T>, bool> _queryHandlerAsync;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IPrepareCommandHandler<SetErrorCountCommand<T>> _prepareCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetErrorCountCommandHandler{T}" /> class.
        /// </summary>
        /// <param name="queryHandler">The query handler.</param>
        /// <param name="queryHandlerAsync">The query handler, asynchronous.</param>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="prepareCommand">The prepare command.</param>
        /// <param name="uniqueIndexQuery">Asks whether the error tracking table carries the unique index that the single-statement path needs.</param>
        /// <param name="tableNameHelper">The table name helper; the index name is derived from the error tracking table's name.</param>
        public SetErrorCountCommandHandler(
            IQueryHandler<GetErrorRecordExistsQuery<T>, bool> queryHandler,
            IQueryHandlerAsync<GetErrorRecordExistsQuery<T>, bool> queryHandlerAsync,
            IDbConnectionFactory dbConnectionFactory,
            IPrepareCommandHandler<SetErrorCountCommand<T>> prepareCommand,
            IQueryHandler<GetErrorTrackingUniqueIndexExistsQuery, bool> uniqueIndexQuery,
            ITableNameHelper tableNameHelper)
        {
            Guard.NotNull(queryHandler);
            Guard.NotNull(uniqueIndexQuery);
            Guard.NotNull(tableNameHelper);
            Guard.NotNull(queryHandlerAsync);
            Guard.NotNull(dbConnectionFactory);
            Guard.NotNull(prepareCommand);

            _queryHandler = queryHandler;
            _canUpsert = new Lazy<bool>(() => uniqueIndexQuery.Handle(
                new GetErrorTrackingUniqueIndexExistsQuery(tableNameHelper.ErrorTrackingName,
                    $"IX_QueueIDExceptionType{tableNameHelper.ErrorTrackingName}")));
            _queryHandlerAsync = queryHandlerAsync;
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
                    if (_canUpsert.Value)
                    {
                        _prepareCommand.Handle(command, commandSql, CommandStringTypes.UpsertErrorCount);
                        commandSql.ExecuteNonQuery();
                        return;
                    }

                    var exists = _queryHandler.Handle(new GetErrorRecordExistsQuery<T>(command.ExceptionType,
                        command.QueueId));
                    Prepare(command, commandSql, exists);
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
                    if (_canUpsert.Value)
                    {
                        _prepareCommand.Handle(command, commandSql, CommandStringTypes.UpsertErrorCount);
                        await commandSql.ExecuteNonQueryAsync().ConfigureAwait(false);
                        return;
                    }

                    var exists = await _queryHandlerAsync
                        .HandleAsync(new GetErrorRecordExistsQuery<T>(command.ExceptionType, command.QueueId))
                        .ConfigureAwait(false);
                    Prepare(command, commandSql, exists);
                    await commandSql.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// An error row is inserted the first time a message fails with a given exception type and
        /// updated on every failure after that.
        /// </summary>
        /// <remarks>
        /// The fallback for a queue whose error tracking table predates the unique index on
        /// (QueueID, ExceptionType). Two failures of the same message arriving together can each see no
        /// row and each insert one, which reads back as a lower retry count than reality - so the message
        /// gets more attempts than configured. Nothing can close that without the index; re-create the
        /// queue to get it.
        /// </remarks>
        private void Prepare(SetErrorCountCommand<T> command, DbCommand commandSql, bool recordExists)
        {
            _prepareCommand.Handle(command, commandSql,
                recordExists ? CommandStringTypes.UpdateErrorCount : CommandStringTypes.InsertErrorCount);
        }
    }
}
