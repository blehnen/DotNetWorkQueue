namespace DotNetWorkQueue.Transport.SqlServer.Schema
{
    /// <summary>
    /// Represents an identity property of a column
    /// </summary>
	public class Identity
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="Identity"/> class.
        /// </summary>
        /// <param name="seed">The seed.</param>
        /// <param name="increment">The increment.</param>
		public Identity(int seed, int increment) 
        {
            Seed = seed;
            Increment = increment;
		}
        #endregion

        #region Public Properties
        /// <summary>
        /// The increment
        /// </summary>
        /// <value>
        /// The increment.
        /// </value>
        public int Increment { get; set; }
        /// <summary>
        /// The seed
        /// </summary>
        /// <value>
        /// The seed.
        /// </value>
        public int Seed { get; set; }
        #endregion

        #region Scripting
        /// <summary>
        /// Translates this identity into a SQL script
        /// </summary>
        /// <returns></returns>
        public string Script() 
        {
			return $"IDENTITY ({Seed},{Increment})";
		}
        #endregion

        #region Clone
        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public Identity Clone()
        {
            return new Identity(Seed, Increment);
        }
        #endregion
    }
}