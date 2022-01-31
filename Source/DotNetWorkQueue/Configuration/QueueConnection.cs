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
using System.Collections.Generic;
namespace DotNetWorkQueue.Configuration
{
    /// <summary>
    /// Defines what queue to use and how to connect to it
    /// </summary>
    public class QueueConnection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueConnection"/> class.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        public QueueConnection(string queue, string connection) : this(queue, connection, null)
        {

        }
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueConnection"/> class.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="additionalConnectionSettings">The additional connection settings.</param>
        public QueueConnection(string queue, string connection, IReadOnlyDictionary<string, string> additionalConnectionSettings)
        {
            Queue = queue;
            Connection = connection;
            AdditionalConnectionSettings = additionalConnectionSettings;
        }

        /// <summary>
        /// Gets the queue.
        /// </summary>
        public string Queue { get; }
        /// <summary>
        /// Gets the connection.
        /// </summary>
        public string Connection { get; }
        /// <summary>
        /// Gets the additional connection settings.
        /// </summary>
        public IReadOnlyDictionary<string, string> AdditionalConnectionSettings { get; }
    }
}
