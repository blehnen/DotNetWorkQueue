// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
namespace DotNetWorkQueue.Transport.SqlServer.Schema
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