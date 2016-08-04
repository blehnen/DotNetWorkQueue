// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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

using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Command
{
    /// <summary>
    /// Saves the meta data - either a new record or an update to an existing one.
    /// </summary>
    public class SaveMetaDataCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SaveMetaDataCommand" /> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="metaData">The meta data.</param>
        public SaveMetaDataCommand(RedisQueueId id, RedisMetaData metaData)
        {
            Guard.NotNull(() => id, id);
            Guard.NotNull(() => metaData, metaData);
            Id = id;
            MetaData = metaData;
        }
        /// <summary>
        /// Gets the meta data.
        /// </summary>
        /// <value>
        /// The meta data.
        /// </value>
        public RedisMetaData MetaData { get; private set; }
        /// <summary>
        /// Gets or sets the queue identifier.
        /// </summary>
        /// <value>
        /// The queue identifier.
        /// </value>
        public RedisQueueId Id { get; private set; }
    }
}
