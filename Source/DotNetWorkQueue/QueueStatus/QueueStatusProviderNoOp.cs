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
using System.Linq;

namespace DotNetWorkQueue.QueueStatus
{
    /// <summary>
    /// A NoOp status provider
    /// </summary>
    internal class QueueStatusProviderNoOp: IQueueStatusProvider
    {
        private readonly QueueInformation _queueInformation;
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueStatusProviderNoOp" /> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        public QueueStatusProviderNoOp(IConnectionInformation connectionInformation)
        {
            Name = connectionInformation.QueueName;
            Server = connectionInformation.Server;
            _queueInformation = new QueueInformation(Name, Server, DateTime.MinValue, string.Empty,
                Enumerable.Empty<SystemEntry>());
        }

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
        /// Gets the last error that occurred, if any.
        /// </summary>
        /// <value>
        /// The error.
        /// </value>
        public Exception Error => null;

        /// <summary>
        /// Handles custom URL paths
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        /// <remarks>Optional. Return null to indicate that this path is not handled by this provider. Otherwise, return a serializable object</remarks>
        public object HandlePath(string path)
        {
            return null;
        }

        /// <summary>
        /// Gets the current queue status / information
        /// </summary>
        /// <value>
        /// The current.
        /// </value>
        public IQueueInformation Current => _queueInformation;
    }
}
