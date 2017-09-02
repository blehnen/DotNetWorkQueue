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

using System;

namespace DotNetWorkQueue.Transport.Memory.Basic
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IQueueStatusProvider" />
    public class QueueStatusProvider : IQueueStatusProvider
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        /// <exception cref="NotImplementedException"></exception>
        public string Name => throw new NotImplementedException();

        /// <summary>
        /// Gets the server.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        /// <exception cref="NotImplementedException"></exception>
        public string Server => throw new NotImplementedException();

        /// <summary>
        /// Gets the last error that occurred, if any.
        /// </summary>
        /// <value>
        /// The error.
        /// </value>
        /// <exception cref="NotImplementedException"></exception>
        public Exception Error => throw new NotImplementedException();

        /// <summary>
        /// Gets the current queue status / information
        /// </summary>
        /// <value>
        /// The current queue information
        /// </value>
        /// <exception cref="NotImplementedException"></exception>
        public IQueueInformation Current => throw new NotImplementedException();

        /// <summary>
        /// Handles custom URL paths
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        /// <remarks>
        /// Optional. Return null to indicate that this path is not handled by this provider. Otherwise, return any serializable object
        /// </remarks>
        public object HandlePath(string path)
        {
            throw new NotImplementedException();
        }
    }
}
