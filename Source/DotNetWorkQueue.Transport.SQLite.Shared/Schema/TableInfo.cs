namespace DotNetWorkQueue.Transport.SQLite.Shared.Schema
{
    /// <summary>
    /// Name and schema owner
    /// </summary>
    public class TableInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TableInfo"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public TableInfo(string name)
        {
            Name = name;
        }
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; }
    }
}
