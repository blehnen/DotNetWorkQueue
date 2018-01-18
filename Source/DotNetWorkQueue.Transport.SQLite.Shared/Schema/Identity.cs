namespace DotNetWorkQueue.Transport.SQLite.Shared.Schema
{
    /// <summary>
    /// Represents an identity property of a column
    /// </summary>
	public class Identity
    {
        #region Scripting
        /// <summary>
        /// Translates this identity into a SQL script
        /// </summary>
        /// <returns></returns>
        public string Script() 
        {
			return "PRIMARY KEY AUTOINCREMENT ";
		}
        #endregion

        #region Clone
        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public Identity Clone()
        {
            return new Identity();
        }
        #endregion
    }
}