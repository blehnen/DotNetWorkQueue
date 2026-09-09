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
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler
{
    /// <inheritdoc />
    /// <summary>
    /// Deletes a message from a queue
    /// </summary>
    internal class DeleteMessageCommandHandlerAsync : ICommandHandlerWithOutputAsync<DeleteMessageCommand<long>, long>
    {
        private readonly Lazy<ITransportOptions> _options;
        private readonly ITransactionFactory _transactionFactory;
        private readonly IPrepareCommandHandler<DeleteMessageCommand<long>> _prepareCommand;
        private readonly IDbConnectionFactory _dbConnectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessageCommandHandlerAsync" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="transactionFactory">The transaction factory.</param>
        /// <param name="prepareCommand">The prepare command.</param>
        public DeleteMessageCommandHandlerAsync(ITransportOptionsFactory options,
            IDbConnectionFactory dbConnectionFactory,
            ITransactionFactory transactionFactory,
            IPrepareCommandHandler<DeleteMessageCommand<long>> prepareCommand)
        {
            Guard.NotNull(options);
            Guard.NotNull(dbConnectionFactory);
            Guard.NotNull(transactionFactory);
            Guard.NotNull(prepareCommand);

            _options = new Lazy<ITransportOptions>(options.Create);
            _transactionFactory = transactionFactory;
            _prepareCommand = prepareCommand;
            _dbConnectionFactory = dbConnectionFactory;
        }

        /// <inheritdoc />
        public async Task<long> HandleAsync(DeleteMessageCommand<long> command)
        {
            using (var connection = _dbConnectionFactory.Create())
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var trans = await _transactionFactory.Create(connection).BeginTransactionAsync().ConfigureAwait(false))
                {
                    using (var commandSql = connection.CreateCommand())
                    {
                        commandSql.Transaction = trans;

                        //delete the meta data record
                        _prepareCommand.Handle(command, commandSql, CommandStringTypes.DeleteFromMetaData);
                        await commandSql.ExecuteNonQueryAsync().ConfigureAwait(false);

                        //delete the message body
                        _prepareCommand.Handle(command, commandSql, CommandStringTypes.DeleteFromQueue);
                        await commandSql.ExecuteNonQueryAsync().ConfigureAwait(false);

                        //delete any error tracking information
                        _prepareCommand.Handle(command, commandSql, CommandStringTypes.DeleteFromErrorTracking);
                        await commandSql.ExecuteNonQueryAsync().ConfigureAwait(false);

                        _prepareCommand.Handle(command, commandSql, CommandStringTypes.DeleteFromMetaDataErrors);
                        await commandSql.ExecuteNonQueryAsync().ConfigureAwait(false);

                        //delete status record
                        if (!_options.Value.EnableStatusTable)
                        {
                            await trans.CommitAsync().ConfigureAwait(false);
                            return 1;
                        }

                        _prepareCommand.Handle(command, commandSql, CommandStringTypes.DeleteFromStatus);
                        await commandSql.ExecuteNonQueryAsync().ConfigureAwait(false);
                        await trans.CommitAsync().ConfigureAwait(false);
                        return 1;
                    }
                }
            }
        }
    }
}
