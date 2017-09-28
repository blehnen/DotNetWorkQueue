namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic
{
    /// <summary>
    /// Contains location information for a Sqlite DB.
    /// </summary>
    public class ConnectionStringInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionStringInfo"/> class.
        /// </summary>
        /// <param name="inMemory">if set to <c>true</c> [in memory].</param>
        /// <param name="fileName">Name of the file.</param>
        public ConnectionStringInfo(bool inMemory, string fileName)
        {
            IsInMemory = inMemory;
            FileName = fileName;
        }
        /// <summary>
        /// Gets a value indicating whether this instance is in memory.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is in memory; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>If true, <seealso cref="FileName"/> will be empty </remarks>
        public bool IsInMemory { get; }
        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <value>
        /// The name of the file.
        /// </value>
        public string FileName { get; }

        /// <summary>
        /// Returns true if the filename is valid or this is an in-memory database
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
        /// </value>
        public bool IsValid => IsInMemory || !string.IsNullOrWhiteSpace(FileName);
    }
}
