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
using System;

namespace DotNetWorkQueue.Transport.SQLite.Schema
{
    /// <summary>
    /// Represents a column in SQLite
    /// </summary>
    public class Column
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Column"/> class.
        /// </summary>
		public Column()
        {

        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Column" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <param name="null">if set to <c>true</c> [null].</param>
        /// <param name="default">The default.</param>
        public Column(string name, ColumnTypes type, bool @null, Default @default)
        {
            Name = name;
            Type = type;
            Default = @default;
            Nullable = @null;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Column" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <param name="length">The length.</param>
        /// <param name="null">if set to <c>true</c> [null].</param>
        /// <param name="default">The default.</param>
        public Column(string name, ColumnTypes type, int length, bool @null, Default @default)
            : this(name, type, @null, @default)
        {
            Length = length;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Column" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <param name="precision">The precision.</param>
        /// <param name="scale">The scale.</param>
        /// <param name="null">if set to <c>true</c> [null].</param>
        /// <param name="default">The default.</param>
        public Column(string name, ColumnTypes type, byte precision, int scale, bool @null, Default @default)
            : this(name, type, @null, @default)
        {
            Precision = precision;
            Scale = scale;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets or sets the default.
        /// </summary>
        /// <value>
        /// The default.
        /// </value>
        public Default Default { get; set; }
        /// <summary>
        /// Gets or sets the identity.
        /// </summary>
        /// <value>
        /// The identity.
        /// </value>
        public Identity Identity { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Column"/> is nullable.
        /// </summary>
        /// <value>
        ///   <c>true</c> if nullable; otherwise, <c>false</c>.
        /// </value>
        public bool Nullable { get; set; }
        /// <summary>
        /// Gets or sets the length.
        /// </summary>
        /// <value>
        /// The length.
        /// </value>
        /// <remarks>Use -1 for 'max'</remarks>
        public int Length { get; set; }
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the position.
        /// </summary>
        /// <value>
        /// The position.
        /// </value>
        public int Position { get; set; }
        /// <summary>
        /// Gets or sets the precision.
        /// </summary>
        /// <value>
        /// The precision.
        /// </value>
        public byte Precision { get; set; }
        /// <summary>
        /// Gets or sets the scale.
        /// </summary>
        /// <value>
        /// The scale.
        /// </value>
        public int Scale { get; set; }
        /// <summary>
        /// Gets or sets the type of the column
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public ColumnTypes Type { get; set; }
        #endregion

        #region Script helpers
        /// <summary>
        /// Gets the nullable text.
        /// </summary>
        /// <value>
        /// The nullable text.
        /// </value>
        private string NullableText => Nullable ? "NULL" : "NOT NULL";

        /// <summary>
        /// Gets the default text.
        /// </summary>
        /// <value>
        /// The default text.
        /// </value>
		private string DefaultText => Default == null ? "" : Environment.NewLine + " " + Default.Script();

        /// <summary>
        /// Gets the identity text.
        /// </summary>
        /// <value>
        /// The identity text.
        /// </value>
		private string IdentityText => Identity == null ? "" : Environment.NewLine + " " + Identity.Script();

        #endregion

        #region Scripting
        /// <summary>
        /// Translates this column into SQL script.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException">SQL data type is not supported.</exception>
        public string Script()
        {
            switch (Type)
            {
                case ColumnTypes.Real:
                case ColumnTypes.Text:
                case ColumnTypes.Integer:
                case ColumnTypes.Blob:
                    return $"[{Name}] [{Type}] {NullableText} {DefaultText} {IdentityText}";
                default:
                    throw new NotSupportedException($"SQL data type {Type} is not supported.");
            }
        }
        #endregion

        #region Clone
        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public Column Clone()
        {
            var rc = new Column { Default = Default };

            if (Identity != null)
            {
                rc.Identity = Identity.Clone();
            }
            rc.Nullable = Nullable;
            rc.Length = Length;
            rc.Name = Name;
            rc.Position = Position;
            rc.Precision = Precision;
            rc.Scale = Scale;
            rc.Type = Type;
            return rc;
        }
        #endregion
    }

    /// <summary>
    /// List of possible column types
    /// </summary>
    public enum ColumnTypes
    {
        /// <summary>
        /// An int
        /// </summary>
        Integer,
        /// <summary>
        /// A real
        /// </summary>
	    Real,
        /// <summary>
        /// A text string
        /// </summary>
		Text,
        /// <summary>
        /// A binary Blob
        /// </summary>
        Blob
    }
}