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
using System;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <inheritdoc />
    public class MessageCorrelationId: ICorrelationId
    {
        private Guid _id;
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageCorrelationId"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public MessageCorrelationId(Guid id)
        {
            _id = id;
            Id = new Setting<Guid>(id);
        }
        /// <inheritdoc />
        public ISetting Id
        {
            get;
            set;
        }
        /// <inheritdoc />
        public bool HasValue => _id != Guid.Empty;

        /// <inheritdoc />
        public override string ToString()
        {
            return _id.ToString();
        }
    }
}
