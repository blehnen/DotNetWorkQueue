using System.Data;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.SQLite.Shared;
using Microsoft.Data.Sqlite;

namespace DotNetWorkQueue.Transport.SQLite.Microsoft.Basic
{
    /// <summary>
    /// A async <see cref="IDbCommand"/> wrapper that allows async usage
    /// </summary>
    public class ReaderAsync: IReaderAsync
    {
        /// <inheritdoc />
        public async Task<int> ExecuteNonQueryAsync(IDbCommand command)
        {
            var sqlCommand = (SqliteCommand) command;
            return await sqlCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<object> ExecuteScalarAsync(IDbCommand command)
        {
            var sqlCommand = (SqliteCommand)command;
            return await sqlCommand.ExecuteScalarAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IDataReader> ExecuteReaderAsync(IDbCommand command)
        {
            var sqlCommand = (SqliteCommand)command;
            return await sqlCommand.ExecuteReaderAsync().ConfigureAwait(false);
        }
    }
}
