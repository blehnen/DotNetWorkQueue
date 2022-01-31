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
using System.Data;
using System.Globalization;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Transport.RelationalDatabase.IReadColumn" />
    public class ReadColumn : RelationalDatabase.Basic.ReadColumn
    {
        /// <summary>
        /// Reads as string.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="column">The column, if known. -1 if the caller has no idea.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="noValue"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override string ReadAsString(CommandStringTypes command, int column, IDataReader reader, string noValue = null)
        {
            switch (command)
            {
                case CommandStringTypes.GetColumnNamesFromTable:
                    return reader.GetString(1); //sqlite puts column name in column 1, not 0
                default:
                    return base.ReadAsString(command, column, reader, noValue);
            }
        }

        /// <summary>
        /// Reads as date time.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="column">The column, if known. -1 if the caller has no idea.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="noValue"></param>
        /// <returns></returns>
        public override DateTime ReadAsDateTime(CommandStringTypes command, int column, IDataReader reader, DateTime noValue = default(DateTime))
        {
            switch (command)
            {
                case CommandStringTypes.GetHeartBeatExpiredMessageIds:
                    return DateTime.FromBinary(reader.GetInt64(column));
                default:
                    return base.ReadAsDateTime(command, column, reader, noValue);
            }
        }

        /// <summary>
        /// Reads as a date time offset
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="column">The column, if known. -1 if the caller has no idea.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="noValue"></param>
        /// <returns></returns>
        public override DateTimeOffset ReadAsDateTimeOffset(CommandStringTypes command, int column, IDataReader reader, DateTimeOffset noValue = default(DateTimeOffset))
        {
            switch (command)
            {
                case CommandStringTypes.DoesJobExist:
                case CommandStringTypes.GetJobLastKnownEvent:
                case CommandStringTypes.GetJobLastScheduleTime:
                    return DateTimeOffset.Parse(reader.GetString(column),
                        CultureInfo.InvariantCulture);
                default:
                    return base.ReadAsDateTimeOffset(command, column, reader, noValue);
            }
        }
    }
}
