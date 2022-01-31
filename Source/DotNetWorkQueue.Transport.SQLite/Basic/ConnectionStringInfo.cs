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
namespace DotNetWorkQueue.Transport.SQLite.Basic
{
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
        /// Returns true if the filename is valid or this is an in-memory database
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
        /// </value>
        public bool IsValid => IsInMemory || !string.IsNullOrWhiteSpace(FileName);
    }
}
