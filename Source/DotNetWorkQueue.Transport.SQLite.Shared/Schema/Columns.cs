using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Schema
{
    /// <summary>
    /// Contains a collection of <see cref="Column"/> classes.
    /// </summary>
	public class Columns
    {
        #region Member level variables
        private readonly List<Column> _columns = new List<Column>();
        #endregion

        #region Collection Methods / Properties
        /// <summary>
        /// Returns the current column list
        /// </summary>
        /// <value>
        /// The items.
        /// </value>
        public ReadOnlyCollection<Column> Items => _columns.AsReadOnly();

        /// <summary>
        /// Adds a new column
        /// </summary>
        /// <param name="column">The column.</param>
		public void Add(Column column) 
        {
            _columns.Add(column);
		}

        /// <summary>
        /// Removes the specified column.
        /// </summary>
        /// <param name="column">The column.</param>
        public void Remove(Column column) 
        {
            _columns.Remove(column);
		}
        #endregion

        #region Scripting
        /// <summary>
        /// Translates this list of columns into SQL script.
        /// </summary>
        /// <returns></returns>
		public string Script() 
        {
            var text = new StringBuilder();
			foreach (var c in _columns) 
            {
				text.Append("   " + c.Script());
                if (_columns.IndexOf(c) < _columns.Count - 1) 
                {
					text.AppendLine(",");
				}
				else 
                {
					text.AppendLine();
				}
			}
			return text.ToString();
        }
        #endregion
    }
}