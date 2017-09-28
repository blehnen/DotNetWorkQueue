using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.SQLite.Shared;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    public class ReaderAsync: IReaderAsync
    {
        public async Task<int> ExecuteNonQueryAsync(IDbCommand command)
        {
            var sqlCommand = (SQLiteCommand) command;
            return await sqlCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        public async Task<object> ExecuteScalarAsync(IDbCommand command)
        {
            var sqlCommand = (SQLiteCommand)command;
            return await sqlCommand.ExecuteScalarAsync().ConfigureAwait(false);
        }

        public async Task<IDataReader> ExecuteReaderAsync(IDbCommand command)
        {
            var sqlCommand = (SQLiteCommand)command;
            return await sqlCommand.ExecuteReaderAsync().ConfigureAwait(false);
        }
    }
}
