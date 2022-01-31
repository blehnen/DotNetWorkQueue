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

namespace DotNetWorkQueue.Transport.PostgreSQL.Schema
{
    /// <summary>
    /// Represents a column
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
        public Column(string name, ColumnTypes type, bool @null)
        {
            Name = name;
            Type = type;
            Nullable = @null;
        }
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="Column" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <param name="length">The length.</param>
        /// <param name="null">if set to <c>true</c> [null].</param>
        public Column(string name, ColumnTypes type, int length, bool @null)
            : this(name, type, @null)
        {
            Length = length;
        }
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="Column" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <param name="precision">The precision.</param>
        /// <param name="scale">The scale.</param>
        /// <param name="null">if set to <c>true</c> [null].</param>
        public Column(string name, ColumnTypes type, byte precision, int scale, bool @null)
            : this(name, type, @null)
        {
            Precision = precision;
            Scale = scale;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets or sets the identity.
        /// </summary>
        /// <value>
        /// The identity.
        /// </value>
        public bool Identity { get; set; }
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
                    if (Identity)
                    {
                        return $"{Name} bigserial {NullableText}";
                    }
                    return $"{Name} {Type} {NullableText}";
                case ColumnTypes.Integer:
                    if (Identity)
                    {
                        return $"{Name} serial {NullableText}";
                    }
                    return $"{Name} {Type} {NullableText}";
                case ColumnTypes.Smallint:
                    if (Identity)
                    {
                        return $"{Name} smallserial {NullableText}";
                    }
                    return $"{Name} {Type} {NullableText}";
                case ColumnTypes.Numeric:
                    return $"{Name} {Type}({Precision},{Scale} {NullableText}";
                case ColumnTypes.Varchar:
                case ColumnTypes.Varbit:
                    var lengthString = Length.ToString();
                    return $"{Name} {Type}({lengthString}) {NullableText}";
                case ColumnTypes.Money:
                case ColumnTypes.Char:
                case ColumnTypes.Text:
                case ColumnTypes.Bytea:
                case ColumnTypes.Date:
                case ColumnTypes.Time:
                case ColumnTypes.Timestamp:
                case ColumnTypes.TimestampTZ:
                case ColumnTypes.TimeTZ:
                case ColumnTypes.Uuid:
                case ColumnTypes.Xml:
                case ColumnTypes.Json:
                case ColumnTypes.Jsonb:
                case ColumnTypes.Real:
                case ColumnTypes.Double:
                case ColumnTypes.Boolean:
                case ColumnTypes.Bit:
                case ColumnTypes.MacAddr:
                    return $"{Name} {Type} {NullableText}";
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
            var rc = new Column
            {
                Identity = Identity,
                Nullable = Nullable,
                Length = Length,
                Name = Name,
                Position = Position,
                Precision = Precision,
                Scale = Scale,
                Type = Type
            };

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
        /// Corresponds to the PostgreSQL 8-byte "bigint" type.
        /// </summary>
        /// <remarks>See http://www.postgresql.org/docs/current/static/datatype-numeric.html</remarks>
        Bigint = 1,

        /// <summary>
        /// Corresponds to the PostgreSQL 8-byte floating-point "double" type.
        /// </summary>
        /// <remarks>See http://www.postgresql.org/docs/current/static/datatype-numeric.html</remarks>
        Double = 8,

        /// <summary>
        /// Corresponds to the PostgreSQL 4-byte "integer" type.
        /// </summary>
        /// <remarks>See http://www.postgresql.org/docs/current/static/datatype-numeric.html</remarks>
        Integer = 9,

        /// <summary>
        /// Corresponds to the PostgreSQL arbitrary-precision "numeric" type.
        /// </summary>
        /// <remarks>See http://www.postgresql.org/docs/current/static/datatype-numeric.html</remarks>
        Numeric = 13,

        /// <summary>
        /// Corresponds to the PostgreSQL floating-point "real" type.
        /// </summary>
        /// <remarks>See http://www.postgresql.org/docs/current/static/datatype-numeric.html</remarks>
        Real = 17,

        /// <summary>
        /// Corresponds to the PostgreSQL 2-byte "smallint" type.
        /// </summary>
        /// <remarks>See http://www.postgresql.org/docs/current/static/datatype-numeric.html</remarks>
        Smallint = 18,

        /// <summary>
        /// Corresponds to the PostgreSQL "boolean" type.
        /// </summary>
        /// <remarks>See http://www.postgresql.org/docs/current/static/datatype-boolean.html</remarks>
        Boolean = 2,

        /// <summary>
        /// Corresponds to the PostgreSQL "money" type.
        /// </summary>
        /// <remarks>See http://www.postgresql.org/docs/current/static/datatype-money.html</remarks>
        Money = 12,

        /// <summary>
        /// Corresponds to the PostgreSQL "char(n)"type.
        /// </summary>
        /// <remarks>See http://www.postgresql.org/docs/current/static/datatype-character.html</remarks>
        Char = 6,

        /// <summary>
        /// Corresponds to the PostgreSQL "text" type.
        /// </summary>
        /// <remarks>See http://www.postgresql.org/docs/current/static/datatype-character.html</remarks>
        Text = 19,

        /// <summary>
        /// Corresponds to the PostgreSQL "varchar" type.
        /// </summary>
        /// <remarks>See http://www.postgresql.org/docs/current/static/datatype-character.html</remarks>
        Varchar = 22,

        /// <summary>
        /// Corresponds to the PostgreSQL "bytea" type, holding a raw byte string.
        /// </summary>
        /// <remarks>See http://www.postgresql.org/docs/current/static/datatype-binary.html</remarks>
        Bytea = 4,

        /// <summary>
        /// Corresponds to the PostgreSQL "date" type.
        /// </summary>
        /// <remarks>See http://www.postgresql.org/docs/current/static/datatype-datetime.html</remarks>
        Date = 7,

        /// <summary>
        /// Corresponds to the PostgreSQL "time" type.
        /// </summary>
        /// <remarks>See http://www.postgresql.org/docs/current/static/datatype-datetime.html</remarks>
        Time = 20,

        /// <summary>
        /// Corresponds to the PostgreSQL "timestamp" type.
        /// </summary>
        /// <remarks>See http://www.postgresql.org/docs/current/static/datatype-datetime.html</remarks>
        Timestamp = 21,

        /// <summary>
        /// Corresponds to the PostgreSQL "timestamp with time zone" type.
        /// </summary>
        /// <remarks>See http://www.postgresql.org/docs/current/static/datatype-datetime.html</remarks>
        // ReSharper disable once InconsistentNaming
        TimestampTZ = 26,

        /// <summary>
        /// Corresponds to the PostgreSQL "time with time zone" type.
        /// </summary>
        /// <remarks>See http://www.postgresql.org/docs/current/static/datatype-datetime.html</remarks>
        // ReSharper disable once InconsistentNaming
        TimeTZ = 31,

        /// <summary>
        /// Corresponds to the PostgreSQL "macaddr" type, a field storing a 6-byte physical address.
        /// </summary>
        /// <remarks>See http://www.postgresql.org/docs/current/static/datatype-net-types.html</remarks>
        MacAddr = 34,

        /// <summary>
        /// Corresponds to the PostgreSQL "bit" type.
        /// </summary>
        /// <remarks>See http://www.postgresql.org/docs/current/static/datatype-bit.html</remarks>
        Bit = 25,

        /// <summary>
        /// Corresponds to the PostgreSQL "varbit" type, a field storing a variable-length string of bits.
        /// </summary>
        /// <remarks>See http://www.postgresql.org/docs/current/static/datatype-boolean.html</remarks>
        Varbit = 39,

        /// <summary>
        /// Corresponds to the PostgreSQL "uuid" type.
        /// </summary>
        /// <remarks>See http://www.postgresql.org/docs/current/static/datatype-uuid.html</remarks>
        Uuid = 27,

        /// <summary>
        /// Corresponds to the PostgreSQL "xml" type.
        /// </summary>
        /// <remarks>See http://www.postgresql.org/docs/current/static/datatype-xml.html</remarks>
        Xml = 28,

        /// <summary>
        /// Corresponds to the PostgreSQL "json" type, a field storing JSON in text format.
        /// </summary>
        /// <remarks>See http://www.postgresql.org/docs/current/static/datatype-json.html</remarks>
        /// <seealso cref="Jsonb"/>
        Json = 35,

        /// <summary>
        /// Corresponds to the PostgreSQL "jsonb" type, a field storing JSON in an optimized binary
        /// format.
        /// </summary>
        /// <remarks>
        /// Supported since PostgreSQL 9.4.
        /// See http://www.postgresql.org/docs/current/static/datatype-json.html
        /// </remarks>
        Jsonb = 36
    }
}