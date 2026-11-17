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
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Decorator
{
    /// <summary>
    /// Deletes nothing when the database file is gone, rather than failing on it.
    /// </summary>
    /// <remarks>
    /// The twin of <see cref="DeleteMessageCommandDecorator"/> for the asynchronous consumer. SQLite is the one transport
    /// whose store can simply disappear - a deleted file - and the synchronous path declines the
    /// command when it does.
    /// </remarks>
    public class DeleteMessageCommandDecoratorAsync : ICommandHandlerWithOutputAsync<DeleteMessageCommand<long>, long>
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly ICommandHandlerWithOutputAsync<DeleteMessageCommand<long>, long> _decorated;
        private readonly DatabaseExists _databaseExists;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessageCommandDecoratorAsync" /> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="decorated">The decorated.</param>
        /// <param name="databaseExists">The database exists.</param>
        public DeleteMessageCommandDecoratorAsync(IConnectionInformation connectionInformation,
            ICommandHandlerWithOutputAsync<DeleteMessageCommand<long>, long> decorated,
            DatabaseExists databaseExists)
        {
            Guard.NotNull(decorated);
            Guard.NotNull(connectionInformation);
            Guard.NotNull(databaseExists);
            _connectionInformation = connectionInformation;
            _decorated = decorated;
            _databaseExists = databaseExists;
        }

        /// <inheritdoc />
        public async Task<long> HandleAsync(DeleteMessageCommand<long> command)
        {
            if (!_databaseExists.Exists(_connectionInformation.ConnectionString)) return 0;
            return await _decorated.HandleAsync(command).ConfigureAwait(false);
        }
    }
}
