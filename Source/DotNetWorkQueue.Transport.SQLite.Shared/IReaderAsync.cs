using System.Data;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Transport.SQLite.Shared
{
    /// <summary>
    /// A async <see cref="IDbCommand"/> wrapper that allows async usage
    /// </summary>
    public interface IReaderAsync
    {
        /// <summary>
        /// Executes the non query asynchronous.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        Task<int> ExecuteNonQueryAsync(IDbCommand command);
        /// <summary>
        /// Executes a scalar method asynchronous.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        Task<object> ExecuteScalarAsync(IDbCommand command);
        /// <summary>
        /// Executes the reader asynchronous.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        Task<IDataReader> ExecuteReaderAsync(IDbCommand command);
    }
}
