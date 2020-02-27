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
using System.Globalization;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <inheritdoc />
    public class MessageQueueId: IMessageId
    {
        private readonly long _id;
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageQueueId"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public MessageQueueId(long id)
        {
            _id = id;
            Id = new Setting<long>(id);
        }
        /// <inheritdoc />
        public ISetting Id { get; }
        /// <inheritdoc />
        public bool HasValue => _id > 0;
        /// <inheritdoc />
        public override string ToString()
        {
            return _id.ToString(CultureInfo.InvariantCulture);
        }
    }
}
