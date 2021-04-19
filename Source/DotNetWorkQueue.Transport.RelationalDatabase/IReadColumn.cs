// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;

namespace DotNetWorkQueue.Transport.RelationalDatabase
{
    /// <summary>
    /// Allows reading data from the data store, without knowing what type of database is being used.
    /// </summary>
    public interface IReadColumn
    {
        /// <summary>
        /// Reads data as a string
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="column">The column, if known. -1 if the caller has no idea.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="noValue">What to return if no value is found</param>
        /// <returns></returns>
        string ReadAsString(CommandStringTypes command, int column, IDataReader reader, string noValue = null);

        /// <summary>
        /// Reads as int32
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="column">The column, if known. -1 if the caller has no idea.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="noValue">What to return if no value is found</param>
        /// <returns></returns>
        int ReadAsInt32(CommandStringTypes command, int column, IDataReader reader, int noValue = 0);

        /// <summary>
        /// Reads as int64
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="column">The column, if known. -1 if the caller has no idea.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="noValue">What to return if no value is found</param>
        /// <returns></returns>
        long ReadAsInt64(CommandStringTypes command, int column, IDataReader reader, long noValue = 0);

        /// <summary>
        /// Reads as int64
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="column">The column, if known. -1 if the caller has no idea.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="noValue">What to return if no value is found</param>
        /// <returns></returns>
        T ReadAsType<T>(CommandStringTypes command, int column, IDataReader reader, T noValue = default);

        /// <summary>
        /// Reads as a date time offset
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="column">The column, if known. -1 if the caller has no idea.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="noValue">What to return if no value is found</param>
        /// <returns></returns>
        DateTimeOffset ReadAsDateTimeOffset(CommandStringTypes command, int column, IDataReader reader, DateTimeOffset noValue = default(DateTimeOffset));

        /// <summary>
        /// Reads as date time.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="column">The column, if known. -1 if the caller has no idea.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="noValue">What to return if no value is found</param>
        /// <returns></returns>
        DateTime ReadAsDateTime(CommandStringTypes command, int column, IDataReader reader, DateTime noValue = default(DateTime));

        /// <summary>
        /// Reads as byte[]
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="column">The column, if known. -1 if the caller has no idea.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="noValue">What to return if no value is found</param>
        /// <returns></returns>
        byte[] ReadAsByteArray(CommandStringTypes command, int column, IDataReader reader, byte[] noValue = null);
    }
}
