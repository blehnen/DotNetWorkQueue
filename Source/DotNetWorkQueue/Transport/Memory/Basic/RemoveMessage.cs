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
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Memory.Basic
{
    /// <summary>
    /// Removes a message from the data storage
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IRemoveMessage" />
    public class RemoveMessage: IRemoveMessage
    {
        private readonly IDataStorage _dataStorage;
        /// <summary>Initializes a new instance of the <see cref="RemoveMessage"/> class.</summary>
        /// <param name="dataStorage">The data storage.</param>
        public RemoveMessage(IDataStorage dataStorage)
        {
            Guard.NotNull(() => dataStorage, dataStorage);
            _dataStorage = dataStorage;
        }

        /// <inheritdoc />
        public RemoveMessageStatus Remove(IMessageId id, RemoveMessageReason reason)
        {
            if (id == null || !id.HasValue) return RemoveMessageStatus.NotFound;
            var found = _dataStorage.DeleteMessage((Guid)id.Id.Value);
            if (found)
                return RemoveMessageStatus.Removed;

            return RemoveMessageStatus.NotFound;
        }

        /// <inheritdoc />
        public RemoveMessageStatus Remove(IMessageContext context, RemoveMessageReason reason)
        {
            return Remove(context.MessageId, reason);
        }
    }
}
