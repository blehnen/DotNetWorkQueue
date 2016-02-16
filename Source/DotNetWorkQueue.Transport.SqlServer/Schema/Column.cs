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
using System;
namespace DotNetWorkQueue.Transport.SqlServer.Schema
{
    /// <summary>
    /// Represents a column in SQL server
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
				case ColumnTypes.Bigint:
				case ColumnTypes.Bit:
				case ColumnTypes.Date:
				case ColumnTypes.Datetime:
				case ColumnTypes.Datetime2:
				case ColumnTypes.Datetimeoffset:
				case ColumnTypes.Float:
				case ColumnTypes.Image:
				case ColumnTypes.Int:
				case ColumnTypes.Money:
				case ColumnTypes.Ntext:
				case ColumnTypes.Real:
				case ColumnTypes.Smalldatetime:
				case ColumnTypes.Smallint:
				case ColumnTypes.Smallmoney:
				case ColumnTypes.Sql_variant:
				case ColumnTypes.Text:
				case ColumnTypes.Time:
				case ColumnTypes.Timestamp:
				case ColumnTypes.Tinyint:
				case ColumnTypes.Uniqueidentifier:
				case ColumnTypes.Xml:

					return $"[{Name}] [{Type}] {NullableText} {DefaultText} {IdentityText}";
                case ColumnTypes.Decimal:
                case ColumnTypes.Numeric:

                    return $"[{Name}] [{Type}]({Precision},{Scale}) {NullableText} {DefaultText}";
				case ColumnTypes.Binary:
				case ColumnTypes.Char:
				case ColumnTypes.Nchar:
				case ColumnTypes.Nvarchar:
				case ColumnTypes.Varbinary:
				case ColumnTypes.Varchar:
					var lengthString = Length.ToString();
					if (lengthString == "-1") lengthString = "max";

					return $"[{Name}] [{Type}]({lengthString}) {NullableText} {DefaultText}";
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
            var rc = new Column {Default = Default};

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
        /// bigint
        /// </summary>
        Bigint,
        /// <summary>
        /// The bit
        /// </summary>
        Bit,
        /// <summary>
        /// The date
        /// </summary>
		Date,
        /// <summary>
        /// The datetime
        /// </summary>
		Datetime,
        /// <summary>
        /// The datetime2
        /// </summary>
		Datetime2,
        /// <summary>
        /// The datetimeoffset
        /// </summary>
		Datetimeoffset,
        /// <summary>
        /// The float
        /// </summary>
		Float,
        /// <summary>
        /// The image
        /// </summary>
		Image,
        /// <summary>
        /// The int
        /// </summary>
		Int,
        /// <summary>
        /// The money
        /// </summary>
		Money,
        /// <summary>
        /// The ntext
        /// </summary>
		Ntext,
        /// <summary>
        /// The real
        /// </summary>
	    Real,
        /// <summary>
        /// The smalldatetime
        /// </summary>
        Smalldatetime,
        /// <summary>
        /// The smallint
        /// </summary>
		Smallint,
        /// <summary>
        /// The smallmoney
        /// </summary>
		Smallmoney,
        /// <summary>
        /// The sql_variant
        /// </summary>
        // ReSharper disable once InconsistentNaming //this name is the name used directly in the SQL script - don't change it
	    Sql_variant,
        /// <summary>
        /// The text
        /// </summary>
		Text,
        /// <summary>
        /// The time
        /// </summary>
		Time,
        /// <summary>
        /// The timestamp
        /// </summary>
		Timestamp,
        /// <summary>
        /// The tinyint
        /// </summary>
		Tinyint,
        /// <summary>
        /// The uniqueidentifier
        /// </summary>
		Uniqueidentifier,
        /// <summary>
        /// The XML
        /// </summary>
		Xml,
        /// <summary>
        /// The binary
        /// </summary>
        Binary,
        /// <summary>
        /// The character
        /// </summary>
	    Char,
        /// <summary>
        /// The nchar
        /// </summary>
		Nchar,
        /// <summary>
        /// The nvarchar
        /// </summary>
	    Nvarchar,
        /// <summary>
        /// The varbinary
        /// </summary>
		Varbinary,
        /// <summary>
        /// The varchar
        /// </summary>
		Varchar,
        /// <summary>
        /// The decimal
        /// </summary>
        Decimal,
        /// <summary>
        /// The numeric
        /// </summary>
		Numeric
    }
}