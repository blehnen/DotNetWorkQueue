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
using System.Collections.Generic;
using DotNetWorkQueue.QueueStatus;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Queue status information
    /// </summary>
    public interface IQueueInformation
    {
        /// <summary>
        /// Gets the current date time
        /// </summary>
        /// <value>
        /// The current date time.
        /// </value>
        /// <remarks>This is what the queue considers to be the current date/time</remarks>
        DateTime CurrentDateTime { get; }
        /// <summary>
        /// Gets the date time provider.
        /// </summary>
        /// <value>
        /// The date time provider.
        /// </value>
        /// <remarks>The name of the data provider that the queue is using to obtain the current date/time</remarks>
        string DateTimeProvider { get; }

        /// <summary>
        /// Gets the name of the queue.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        string Name { get; }

        /// <summary>
        /// Gets the server.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        /// <remarks>The transport server name / address</remarks>
        string Server { get; }

        /// <summary>
        /// Gets queue information, such as pending or error counts
        /// </summary>
        /// <value>
        /// The data.
        /// </value>
        IEnumerable<SystemEntry> Data { get; }
    }
}
