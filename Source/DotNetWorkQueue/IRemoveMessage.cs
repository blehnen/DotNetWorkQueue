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
namespace DotNetWorkQueue
{
    /// <summary>
    /// Removes a specific message from the transport
    /// </summary>
    public interface IRemoveMessage
    {
        /// <summary>
        /// Removes a specific message from the transport
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Status of the request</returns>
        RemoveMessageStatus Remove(IMessageId id);

        /// <summary>
        /// Removes a specific message from the transport
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>Status of the request</returns>
        RemoveMessageStatus Remove(IMessageContext context);
    }

    /// <summary>
    /// Indicates the status of a request to remove a message from the queue
    /// </summary>
    public enum RemoveMessageStatus
    {
        /// <summary>
        /// The record was not found
        /// </summary>
        NotFound = 0,
        /// <summary>
        /// The message was removed
        /// </summary>
        Removed = 1
    }
}
