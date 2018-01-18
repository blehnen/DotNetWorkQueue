using System.Data;

namespace DotNetWorkQueue.Transport.SQLite.Shared
{
    /// <summary>
    /// Creates new db objects
    /// </summary>
    public interface IDbFactory
    {
        /// <summary>
        /// Creates the connection.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="forMemoryHold">if set to <c>true</c> [this connection is our master in-memory connection. This connection keeps the in-memory database alive].</param>
        /// <returns></returns>
        IDbConnection CreateConnection(string connectionString, bool forMemoryHold);

        /// <summary>
        /// Creates the command.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        IDbCommand CreateCommand(IDbConnection connection);

        /// <summary>
        /// Creates a new instance of <seealso cref="ISQLiteTransactionWrapper"/>
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        ISQLiteTransactionWrapper CreateTransaction(IDbConnection connection);
    }
}
