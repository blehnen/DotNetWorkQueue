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
using System.Collections.Generic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Memory.Basic
{
    /// <summary>
    /// Obtains the header records for a message
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IGetHeader" />
    public class GetHeader: IGetHeader
    {
        private readonly IDataStorage _dataStorage;
        /// <summary>Initializes a new instance of the <see cref="RemoveMessage"/> class.</summary>
        /// <param name="dataStorage">The data storage.</param>
        public GetHeader(IDataStorage dataStorage)
        {
            Guard.NotNull(() => dataStorage, dataStorage);
            _dataStorage = dataStorage;
        }

        /// <summary>
        /// Gets the headers for the specified message if possible
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>
        /// null if the headers could not be obtained; otherwise a collection with 0 or more records
        /// </returns>
        public IDictionary<string, object> GetHeaders(IMessageId id)
        {
            if (id != null && id.HasValue)
                return _dataStorage.GetHeaders((Guid)id.Id.Value);
            return null;
        }
    }
}
