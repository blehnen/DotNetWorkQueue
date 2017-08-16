// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using DotNetWorkQueue.Exceptions;

namespace DotNetWorkQueue.Transport.PostgreSQL.Schema
{
    /// <summary>
    /// Represents a constraint
    /// </summary>
	public class Constraint
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Constraint"/> class.
        /// </summary>
        public Constraint()
        {
        } 

        /// <summary>
        /// Initializes a new instance of the <see cref="Constraint" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <param name="column">The column.</param>
        public Constraint(string name, ConstraintType type, string column) 
        {
			Name = name;
			Type = type;
            Columns = new List<string> {column};
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Constraint"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <param name="columns">The columns.</param>
        public Constraint(string name, ConstraintType type, List<string> columns)
        {
            Name = name;
            Type = type;
            Columns = columns;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets or sets the columns.
        /// </summary>
        /// <value>
        /// The columns.
        /// </value>
        public List<string> Columns { get; set; }
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the table that this constraint belongs to
        /// </summary>
        /// <value>
        /// The table.
        /// </value>
        public TableInfo Table { get; set; }
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public ConstraintType Type { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Constraint"/> is unique.
        /// </summary>
        /// <value>
        ///   <c>true</c> if unique; otherwise, <c>false</c>.
        /// </value>
        public bool Unique { get; set; }
        #endregion

        #region Script helpers
        /// <summary>
        /// Gets the unique text.
        /// </summary>
        /// <value>
        /// The unique text.
        /// </value>
		private string UniqueText => !Unique ? "" : "UNIQUE";

        #endregion

        #region Scripting
        /// <summary>
        /// Translates this constraint into a SQL script
        /// </summary>
        /// <returns></returns>
        public string Script()
        {
            return Type != ConstraintType.Index 
                ? 
                $"CONSTRAINT {Name} {ConvertToString(Type)} ({string.Join(", ", Columns.ToArray())})" 
                : 
                $"CREATE {UniqueText} INDEX {Name} ON {Table.Name} ({string.Join(", ", Columns.ToArray())})";
        }

        #endregion

        #region Clone
        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public Constraint Clone()
        {
            var temp = new List<string>();
            temp.AddRange(Columns);
            var rc = new Constraint(Name, Type, temp)
            {
                Unique = Unique
            };
            return rc;
        }
        #endregion

        /// <summary>
        /// Converts a constraint to a string
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        private string ConvertToString(ConstraintType type)
        {
            switch(type)
            {
                case ConstraintType.PrimaryKey:
                    return "PRIMARY KEY";
                case ConstraintType.Constraint:
                    if (Unique)
                    {
                        return "Unique";
                    }
                    throw new DotNetWorkQueueException("Only unique constraints are supported; set the unique flag to true. For primary keys, use the primary key type instead. For indexes, specify an index type instead.");
                default:
                    return type.ToString().ToUpperInvariant();
            }
        }
    }
    /// <summary>
    /// The type of the constraint
    /// </summary>
    public enum ConstraintType
    {
        /// <summary>
        /// index
        /// </summary>
        Index,
        /// <summary>
        /// primary key
        /// </summary>
        PrimaryKey,
        /// <summary>
        /// constraint
        /// </summary>
        Constraint
    }
}