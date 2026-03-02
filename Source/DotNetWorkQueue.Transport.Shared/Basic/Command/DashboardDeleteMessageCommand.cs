// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
namespace DotNetWorkQueue.Transport.Shared.Basic.Command
{
    /// <summary>
    /// Dashboard command: deletes a single message by its ID.
    /// All transports implement a handler for this command. Each handler converts
    /// the string MessageId to its native type internally (long for relational, int for LiteDB,
    /// string for Redis).
    /// </summary>
    public class DashboardDeleteMessageCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardDeleteMessageCommand"/> class.
        /// </summary>
        /// <param name="messageId">The message identifier (string — supports both numeric and UUID IDs).</param>
        public DashboardDeleteMessageCommand(string messageId)
        {
            MessageId = messageId;
        }

        /// <summary>
        /// Gets the message identifier.
        /// </summary>
        public string MessageId { get; }
    }
}
