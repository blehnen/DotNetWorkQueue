using System.Data;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Transport.SQLite.Shared
{
    public interface IReaderAsync
    {
        Task<int> ExecuteNonQueryAsync(IDbCommand command);
        Task<object> ExecuteScalarAsync(IDbCommand command);
        Task<IDataReader> ExecuteReaderAsync(IDbCommand command);
    }
}
