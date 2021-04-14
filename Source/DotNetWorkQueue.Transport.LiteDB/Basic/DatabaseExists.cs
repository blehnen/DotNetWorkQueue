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
using System.IO;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.LiteDb.Basic
{
    /// <summary>
    /// Determines if a specified database exists
    /// </summary>
    /// <remarks>The database could be on the file system or in memory</remarks>
    public class DatabaseExists
    {
        private readonly IGetFileNameFromConnectionString _getFileNameFromConnection;
        private readonly IConnectionInformation _connectionInformation;
        private readonly ILogger _logger;
        /// <summary>Initializes a new instance of the <see cref="DatabaseExists"/> class.</summary>
        /// <param name="getFileNameFromConnection">The get file name from connection.</param>
        /// <param name="connectionInformation">Connection info</param>
        /// <param name="logger">Logger</param>
        public DatabaseExists(IGetFileNameFromConnectionString getFileNameFromConnection,
            IConnectionInformation connectionInformation,
            ILogger logger)
        {
            Guard.NotNull(() => getFileNameFromConnection, getFileNameFromConnection);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            _getFileNameFromConnection = getFileNameFromConnection;
            _connectionInformation = connectionInformation;
            _logger = logger;
        }
        /// <summary>
        /// Returns true if the specified database exists
        /// </summary>
        /// <returns></returns>
        public bool Exists()
        {
            var fileName = _getFileNameFromConnection.GetFileName(_connectionInformation.ConnectionString);
            if (fileName.IsInMemory) return true; //memory dbs always exist
            
            var exist = File.Exists(fileName.FileName);
            if(!exist)
                _logger.LogDebug(() => $"database {fileName.FileName} was not found");
            return exist;
        }
    }
}
