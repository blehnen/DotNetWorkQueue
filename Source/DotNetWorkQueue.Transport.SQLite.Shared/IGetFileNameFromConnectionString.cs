using DotNetWorkQueue.Transport.SQLite.Shared.Basic;

namespace DotNetWorkQueue.Transport.SQLite.Shared
{
    /// <summary>
    /// Gets the file name from a connection string
    /// </summary>
    public interface IGetFileNameFromConnectionString
    {
        /// <summary>
        /// Gets the file name from a connection string
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns></returns>
        ConnectionStringInfo GetFileName(string connectionString);
    }
}
