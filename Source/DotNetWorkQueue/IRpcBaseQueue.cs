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
using DotNetWorkQueue.Configuration;
namespace DotNetWorkQueue
{
    /// <summary>
    /// Base interface for sending a message and receiving a reply
    /// </summary>
    public interface IRpcBaseQueue: IQueue
    {
        /// <summary>
        /// The queue configuration
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        QueueRpcConfiguration Configuration { get; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="IRpcBaseQueue"/> is started.
        /// </summary>
        /// <value>
        ///   <c>true</c> if started; otherwise, <c>false</c>.
        /// </value>
        bool Started { get; }

        /// <summary>
        /// Starts the queue.
        /// </summary>
        /// <remarks>This must be called after setting any configuration options, and before sending any messages.</remarks>
        void Start();
    }
}
