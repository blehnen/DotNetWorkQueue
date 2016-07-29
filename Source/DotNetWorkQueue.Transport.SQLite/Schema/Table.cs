// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using System.Linq;
using System.Text;

namespace DotNetWorkQueue.Transport.SQLite.Schema
{
    /// <summary>
    /// Represents a table in SQLite
    /// </summary>
	public class Table
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="Table" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
		public Table(string name) 
        {
			Name = name;
            Columns = new Columns();
            Constraints = new List<Constraint>();
		}
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets or sets the columns.
        /// </summary>
        /// <value>
        /// The columns.
        /// </value>
        public Columns Columns { get; set; }
        /// <summary>
        /// Gets or sets the constraints.
        /// </summary>
        /// <value>
        /// The constraints.
        /// </value>
        public List<Constraint> Constraints { get; set; }
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets the table information.
        /// </summary>
        /// <value>
        /// The information.
        /// </value>
        public TableInfo Info => new TableInfo(Name);

        /// <summary>
        /// Gets the primary key. Will return null if no primary key is defined.
        /// </summary>
        /// <value>
        /// The primary key.
        /// </value>
        public Constraint PrimaryKey 
        {
            get
            {
                return Constraints.FirstOrDefault(c => c.Type == ContraintType.PrimaryKey);
            }
        }

        #endregion

        #region Scripting
        /// <summary>
        /// Translates this table into a SQL script
        /// </summary>
        /// <returns></returns>
		public string Script() 
        {
			var text = new StringBuilder();
			text.AppendFormat("CREATE TABLE [{0}](\r\n", Name);
			text.Append(Columns.Script());
            if (Constraints.Count > 0)
            {
                text.AppendLine();
            }
            //add primary keys
			foreach (var c in Constraints.Where(c => c.Type == ContraintType.PrimaryKey))
			{
			    text.AppendLine("   ," + c.Script());
			}
			text.AppendLine(");");
			text.AppendLine();

            //add indexes and constraints
            foreach (var c in Constraints.Where(c => c.Type != ContraintType.PrimaryKey))
            {
                text.AppendLine(c.Script());
            }
            return text.ToString();
        }
        #endregion

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Name;
        }
    }
}