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
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// A correlation Id that can be serialized
    /// </summary>
    public class RedisQueueCorrelationIdSerialized
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueCorrelationIdSerialized"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public RedisQueueCorrelationIdSerialized(Guid id)
        {
            Id = id;
        }
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public Guid Id { get; set; }
    }

    /// <inheritdoc />
    public class RedisQueueCorrelationId: ICorrelationId
    {
        private Guid _id;
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueCorrelationId"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public RedisQueueCorrelationId(Guid id)
        {
            _id = id;
            Id = new Setting<Guid>(id);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueCorrelationId"/> class.
        /// </summary>
        /// <param name="input">The serialized input.</param>
        public RedisQueueCorrelationId(RedisQueueCorrelationIdSerialized input)
        {
            if(input != null)
            {
                _id = input.Id;
                Id = new Setting<Guid>(input.Id);
            }
            else
            {
                _id = Guid.Empty;
                Id = new Setting<Guid>(_id);
            }
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
