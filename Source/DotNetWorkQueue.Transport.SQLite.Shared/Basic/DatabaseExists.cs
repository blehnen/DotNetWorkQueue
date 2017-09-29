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

using System.IO;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic
{
    /// <summary>
    /// Determines if a specified database exists
    /// </summary>
    /// <remarks>The database could be on the file system or in memory</remarks>
    public class DatabaseExists
    {
        private readonly IGetFileNameFromConnectionString _getFileNameFromConnection;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseExists"/> class.
        /// </summary>
        /// <param name="getFileNameFromConnection">The get file name from connection.</param>
        public DatabaseExists(IGetFileNameFromConnectionString getFileNameFromConnection)
        {
            Guard.NotNull(() => getFileNameFromConnection, getFileNameFromConnection);
            _getFileNameFromConnection = getFileNameFromConnection;
        }
        /// <summary>
        /// Returns true if the specified database exists
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public bool Exists(string connectionString)
        {
            var fileName = _getFileNameFromConnection.GetFileName(connectionString);
            return fileName.IsInMemory || File.Exists(fileName.FileName);
        }
    }
}
