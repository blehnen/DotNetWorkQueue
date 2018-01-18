using System.IO;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic
{
    /// <summary>
    /// Determines if a specified database exists
    /// </summary>
    /// <remarks>The database could be on the file system or in memory</remarks>
    public class DatabaseExists
    {
        private readonly IGetFileNameFromConnectionString _getFileNameFromConnection;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseExists"/> class.
        /// </summary>
        /// <param name="getFileNameFromConnection">The get file name from connection.</param>
        public DatabaseExists(IGetFileNameFromConnectionString getFileNameFromConnection)
        {
            Guard.NotNull(() => getFileNameFromConnection, getFileNameFromConnection);
            _getFileNameFromConnection = getFileNameFromConnection;
        }
        /// <summary>
        /// Returns true if the specified database exists
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public bool Exists(string connectionString)
        {
            var fileName = _getFileNameFromConnection.GetFileName(connectionString);
            return fileName.IsInMemory || File.Exists(fileName.FileName);
        }
    }
}
