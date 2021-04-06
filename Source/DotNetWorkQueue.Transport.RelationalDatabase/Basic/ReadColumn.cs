// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
using System.Data;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <inheritdoc />
    public class ReadColumn : IReadColumn
    {
        /// <inheritdoc />
        public virtual string ReadAsString(CommandStringTypes command, int column, IDataReader reader, string noValue = null)
        {
            ValidColumn(column, command);
            return !reader.IsDBNull(column) ? reader.GetString(column) : noValue;
        }

        /// <inheritdoc />
        public virtual DateTime ReadAsDateTime(CommandStringTypes command, int column, IDataReader reader, DateTime noValue = default(DateTime))
        {
            ValidColumn(column, command);
            return !reader.IsDBNull(column) ? reader.GetDateTime(column) : noValue;
        }

        /// <inheritdoc />
        public virtual int ReadAsInt32(CommandStringTypes command, int column, IDataReader reader, int noValue = 0)
        {
            ValidColumn(column, command);
            return !reader.IsDBNull(column) ? reader.GetInt32(column) : noValue;
        }

        /// <inheritdoc />
        public virtual long ReadAsInt64(CommandStringTypes command, int column, IDataReader reader, long noValue = 0)
        {
            ValidColumn(column, command);
            return !reader.IsDBNull(column) ? reader.GetInt64(column) : noValue;
        }

        /// <inheritdoc />
        public virtual DateTimeOffset ReadAsDateTimeOffset(CommandStringTypes command, int column, IDataReader reader, DateTimeOffset noValue = default(DateTimeOffset))
        {
            ValidColumn(column, command);
            if(!reader.IsDBNull(column))
                return (DateTimeOffset)reader[column];
            return noValue;
        }

        /// <inheritdoc />
        public virtual byte[] ReadAsByteArray(CommandStringTypes command, int column, IDataReader reader, byte[] noValue = null)
        {
            ValidColumn(column, command);
            if (!reader.IsDBNull(column))
                return (byte[])reader[column];
            return noValue;
        }

        /// <summary>
        /// Reads a value from the reader as the specified type
        /// </summary>
        /// <typeparam name="T">the data type</typeparam>
        /// <param name="command">The command.</param>
        /// <param name="column">The column, if known. -1 if the caller has no idea.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="noValue">What to return if no value is found</param>
        /// <returns></returns>
        public virtual T ReadAsType<T>(CommandStringTypes command, int column, IDataReader reader, T noValue = default)
        {
            ValidColumn(column, command);
            if (!reader.IsDBNull(column))
                return (T)reader[column];
            return noValue;
        }

        /// <summary>
        /// Validates that the column can be used.
        /// </summary>
        /// <param name="column">The column.</param>
        /// <param name="command">The command.</param>
        /// <exception cref="ArgumentException">column is -1; can only be handled in overridden implementations</exception>
        protected virtual void ValidColumn(int column, CommandStringTypes command)
        {
            if (column == -1)
                throw new ArgumentException("column is -1; can only be handled in overridden implementations");
        }
    }
}
