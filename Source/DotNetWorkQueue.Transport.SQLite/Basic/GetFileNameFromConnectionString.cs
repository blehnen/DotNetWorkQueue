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
namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// Determines the full path and file name of a Sqlite DB, based on the connection string.
    /// </summary>
    public static class GetFileNameFromConnectionString
    {
        /// <summary>
        /// Gets the full path and file name of a DB. In memory databases will instead set the <seealso cref="ConnectionStringInfo.IsInMemory"/> flag to true.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static ConnectionStringInfo GetFileName(string connectionString)
        {
            System.Data.SQLite.SQLiteConnectionStringBuilder builder;
            try
            {
                 builder = new System.Data.SQLite.SQLiteConnectionStringBuilder(connectionString);
            }
            // ReSharper disable once UncatchableException
            catch (System.ArgumentException) //bad format - return a connectionstring info that isn't valid
            {
                return new ConnectionStringInfo(false, string.Empty);
            }

            var dataSource = builder.DataSource.ToLowerInvariant();
            var inMemory = dataSource.Contains(":memory:") || dataSource.Contains("mode=memory");

            if (inMemory || string.IsNullOrWhiteSpace(builder.FullUri))
                return new ConnectionStringInfo(inMemory, builder.DataSource);

            var uri = builder.FullUri.ToLowerInvariant();
            inMemory = uri.Contains(":memory:") || uri.Contains("mode=memory");

            return new ConnectionStringInfo(inMemory, builder.DataSource);
        }
    }
    /// <summary>
    /// Contains location information for a Sqlite DB.
    /// </summary>
    public class ConnectionStringInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionStringInfo"/> class.
        /// </summary>
        /// <param name="inMemory">if set to <c>true</c> [in memory].</param>
        /// <param name="fileName">Name of the file.</param>
        public ConnectionStringInfo(bool inMemory, string fileName)
        {
            IsInMemory = inMemory;
            FileName = fileName;
        }
        /// <summary>
        /// Gets a value indicating whether this instance is in memory.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is in memory; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>If true, <seealso cref="FileName"/> will be empty </remarks>
        public bool IsInMemory { get; }
        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <value>
        /// The name of the file.
        /// </value>
        public string FileName { get; }

        /// <summary>
        /// Returns true if the filename is valid or this is an inmemory database
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
        /// </value>
        public bool IsValid => IsInMemory || !string.IsNullOrWhiteSpace(FileName);
    }
}
