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
    /// Dashboard command: overwrites the body and headers of a single message in the Queue table.
    /// Both columns must be updated together because interceptors (compression, encryption) write
    /// markers into the headers when encoding.
    /// </summary>
    public class DashboardUpdateMessageBodyCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardUpdateMessageBodyCommand"/> class.
        /// </summary>
        /// <param name="messageId">The message identifier (string — supports both numeric and UUID IDs).</param>
        /// <param name="body">The re-encoded message body bytes.</param>
        /// <param name="headers">The updated message headers bytes (with fresh interceptor markers).</param>
        public DashboardUpdateMessageBodyCommand(string messageId, byte[] body, byte[] headers)
        {
            MessageId = messageId;
            Body = body;
            Headers = headers;
        }

        /// <summary>Gets the message identifier.</summary>
        public string MessageId { get; }

        /// <summary>Gets the re-encoded message body bytes.</summary>
        public byte[] Body { get; }

        /// <summary>Gets the updated headers bytes.</summary>
        public byte[] Headers { get; }
    }
}
