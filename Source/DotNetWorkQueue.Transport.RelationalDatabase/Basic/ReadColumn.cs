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
using System;
using System.Data;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    public class ReadColumn : IReadColumn
    {
        /// <summary>
        /// Reads data as a string
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="column">The column, if known. -1 if the caller has no idea.</param>
        /// <param name="reader">The reader.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual string ReadAsString(CommandStringTypes command, int column, IDataReader reader)
        {
            switch (command)
            {
                case CommandStringTypes.GetColumnNamesFromTable:
                    return reader.GetString(0);
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Reads as date time.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="column">The column, if known. -1 if the caller has no idea.</param>
        /// <param name="reader">The reader.</param>
        /// <returns></returns>
        public virtual DateTime ReadAsDateTime(CommandStringTypes command, int column, IDataReader reader)
        {
            switch (command)
            {
                case CommandStringTypes.GetHeartBeatExpiredMessageIds:
                    return reader.GetDateTime(column);
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
