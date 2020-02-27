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
using DotNetWorkQueue.Transport.SQLite.Shared;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using Microsoft.Data.Sqlite;

namespace DotNetWorkQueue.Transport.SQLite.Microsoft.Basic
{
    /// <summary>
    /// Determines the full path and file name of a Sqlite DB, based on the connection string.
    /// </summary>
    public class GetFileNameFromConnectionString: IGetFileNameFromConnectionString
    {
        /// <summary>
        /// Gets the full path and file name of a DB. In memory databases will instead set the <seealso cref="ConnectionStringInfo.IsInMemory"/> flag to true.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public ConnectionStringInfo GetFileName(string connectionString)
        {
            SqliteConnectionStringBuilder builder;
            try
            {
                 builder = new SqliteConnectionStringBuilder(connectionString);
            }
            // ReSharper disable once UncatchableException
            catch (ArgumentException) //bad format - return a connection string info that isn't valid
            {
                return new ConnectionStringInfo(false, string.Empty);
            }

            var dataSource = builder.DataSource.ToLowerInvariant();
            var inMemory = dataSource.Contains(":memory:") || dataSource.Contains("mode=memory");

            if (inMemory || string.IsNullOrWhiteSpace(builder.ConnectionString))
                return new ConnectionStringInfo(inMemory, builder.DataSource);

            var uri = builder.ConnectionString.ToLowerInvariant();
            inMemory = uri.Contains(":memory:") || uri.Contains("mode=memory");

            return new ConnectionStringInfo(inMemory, builder.DataSource);
        }
    }
}
