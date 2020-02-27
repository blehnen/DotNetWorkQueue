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
namespace DotNetWorkQueue
{
    /// <summary>
    /// Removes a specific message from the transport
    /// </summary>
    public interface IRemoveMessage
    {
        /// <summary>Removes a specific message from the transport</summary>
        /// <param name="id">The identifier.</param>
        /// <param name="reason">The reason for removing the message</param>
        /// <returns>Status of the request</returns>
        RemoveMessageStatus Remove(IMessageId id, RemoveMessageReason reason);

        /// <summary>Removes a specific message from the transport</summary>
        /// <param name="context">The context.</param>
        /// <param name="reason">The reason for removing the message</param>
        /// <returns>Status of the request</returns>
        RemoveMessageStatus Remove(IMessageContext context, RemoveMessageReason reason);
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

    /// <summary>
    /// Reasons why a message was removed
    /// </summary>
    public enum RemoveMessageReason
    {
        /// <summary>
        /// Message Completed
        /// </summary>
        Complete = 0,
        /// <summary>
        /// Message removed due to error
        /// </summary>
        Error = 1,
        /// <summary>
        /// Message has expired
        /// </summary>
        Expired = 2,
    }
}
