// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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

using System.Text;

namespace DotNetWorkQueue.Transport.SqlServer.Schema
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

        #region Clone

        /// <summary>
        /// Returns a copy of this instance
        /// </summary>
        /// <param name="newName">The name to use for the cloned copy, instead of the current name</param>
        /// <returns>A copy of this instance, as a new instance</returns>
        public Default Clone(string newName = null)
        {
            return !string.IsNullOrEmpty(newName) ? new Default(newName, Value) : new Default(Name, Value);
        }
        #endregion
    }
}