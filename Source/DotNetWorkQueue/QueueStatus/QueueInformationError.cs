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
using System;
using System.Collections.Generic;
namespace DotNetWorkQueue.QueueStatus
{
    /// <summary>
    /// Returns error information, resulting from trying to query a queue for status information
    /// </summary>
    internal class QueueInformationError : IQueueInformation
    {
        private readonly Exception _error;
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueInformationError"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="server">The server.</param>
        /// <param name="error">The error.</param>
        public QueueInformationError(string name, string server, Exception error)
        {
            Name = name;
            Server = server;
            _error = error;
        }
        /// <summary>
        /// Gets or sets the curent date time.
        /// </summary>
        /// <value>
        /// The curent date time.
        /// </value>
        public DateTime CurentDateTime => DateTime.MinValue;
        /// <summary>
        /// Gets or sets the date time provider.
        /// </summary>
        /// <value>
        /// The date time provider.
        /// </value>
        public string DateTimeProvider => string.Empty;
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; }
        /// <summary>
        /// Gets the server.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        public string Server { get; }
        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <value>
        /// The data.
        /// </value>
        public IEnumerable<SystemEntry> Data { get { yield return new SystemEntry("Error", _error.ToString()); } }
    }
}
