namespace DotNetWorkQueue.Transport.SqlServer.Schema
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
        /// <param name="owner">The owner.</param>
        public TableInfo(string name, string owner)
        {
            Name = name;
            Owner = owner;
        }
        /// <summary>
        /// Gets the owner.
        /// </summary>
        /// <value>
        /// The owner.
        /// </value>
        public string Owner { get;  }
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; }
    }
}
