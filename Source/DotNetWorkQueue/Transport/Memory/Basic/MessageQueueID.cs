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
using System;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue.Transport.Memory.Basic
{
    /// <summary>
    /// The primary ID for a message in the transport
    /// </summary>
    public class MessageQueueId : IMessageId
    {
        private readonly Guid _id;
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageQueueId"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public MessageQueueId(Guid id)
        {
            _id = id;
            Id = new Setting<Guid>(id);
        }
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public ISetting Id { get; }
        /// <summary>
        /// Gets a value indicating if <see cref="Id" /> is not null / not empty
        /// </summary>
        /// <value>
        /// <c>true</c> if <see cref="Id" /> has value; otherwise, <c>false</c>.
        /// </value>
        public bool HasValue => _id != Guid.Empty;

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return _id.ToString();
        }
    }
}
