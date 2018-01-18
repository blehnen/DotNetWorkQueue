namespace DotNetWorkQueue.Transport.SQLite.Shared.Schema
{
    /// <summary>
    /// Represents a default value for a column
    /// </summary>
	public class Default
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="Default"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
		public Default(string name, string value) 
        {
			Name = name;
			Value = value;
		}
        #endregion

        #region Public properties
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public string Value { get; set; }

        #endregion

        #region Scripting
        /// <summary>
        /// Translates this default into a SQL script
        /// </summary>
        /// <returns></returns>
		public string Script() 
        {
			return $"CONSTRAINT [{Name}] DEFAULT {Value}";
        }
        #endregion
    }
}