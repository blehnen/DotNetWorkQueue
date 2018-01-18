// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
    /// Basic information about a queue
    /// </summary>
    /// <remarks>Detailed information is up to the transport - see <seealso cref="Data"/> </remarks>
    public class QueueInformation: IQueueInformation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueInformation" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="server">The server.</param>
        /// <param name="currentDateTime">The current date time.</param>
        /// <param name="dateTimeProvider">The date time provider.</param>
        /// <param name="data">The data.</param>
        public QueueInformation(string name,
            string server,
            DateTime currentDateTime, 
            string dateTimeProvider,
            IEnumerable<SystemEntry> data)
        {
            Name = name;
            Server = server;
            CurrentDateTime = currentDateTime;
            DateTimeProvider = dateTimeProvider;
            Data = data;
        }

        /// <summary>
        /// Gets the name of the queue.
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
        /// Gets the current date time
        /// </summary>
        /// <value>
        /// The current date time.
        /// </value>
        /// <remarks>
        /// This is what the current time value is for the queue itself
        /// </remarks>
        public DateTime CurrentDateTime { get; }

        /// <summary>
        /// Gets the date time provider.
        /// </summary>
        /// <value>
        /// The date time provider.
        /// </value>
        public string DateTimeProvider { get; }

        /// <summary>
        /// Gets queue information, such as pending or error counts
        /// </summary>
        /// <value>
        /// The data.
        /// </value>
        public IEnumerable<SystemEntry> Data { get; }
    }
}
