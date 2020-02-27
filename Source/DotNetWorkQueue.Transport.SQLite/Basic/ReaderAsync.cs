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
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.SQLite.Shared;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// A async <see cref="IDbCommand"/> wrapper that allows async usage
    /// </summary>
    public class ReaderAsync: IReaderAsync
    {
        /// <inheritdoc />
        public async Task<int> ExecuteNonQueryAsync(IDbCommand command)
        {
            var sqlCommand = (SQLiteCommand) command;
            return await sqlCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<object> ExecuteScalarAsync(IDbCommand command)
        {
            var sqlCommand = (SQLiteCommand)command;
            return await sqlCommand.ExecuteScalarAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IDataReader> ExecuteReaderAsync(IDbCommand command)
        {
            var sqlCommand = (SQLiteCommand)command;
            return await sqlCommand.ExecuteReaderAsync().ConfigureAwait(false);
        }
    }
}
