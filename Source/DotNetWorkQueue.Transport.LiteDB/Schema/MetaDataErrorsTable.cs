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
using DotNetWorkQueue.Transport.LiteDb.Basic;
using LiteDB;

namespace DotNetWorkQueue.Transport.LiteDb.Schema
{
    /// <summary>
    /// Error table for meta data record
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Transport.LiteDb.Basic.ITable" />
    public class MetaDataErrorsTable : ITable
    {
        /// <inheritdoc />
        public bool Create(LiteDbConnectionManager connection, LiteDbMessageQueueTransportOptions options,
            TableNameHelper helper)
        {
            using (var db = connection.GetDatabase())
            {
                var col = db.Database.GetCollection<MetaDataErrorsTable>(helper.MetaDataErrorsName);

                col.EnsureIndex(x => x.Id);
                col.EnsureIndex(x => x.QueueId, true);

                return true;
            }

        }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public int Id { get; set; }
        /// <summary>
        /// Gets or sets the queue identifier.
        /// </summary>
        /// <value>
        /// The queue identifier.
        /// </value>
        public int QueueId { get; set; }
        /// <summary>
        /// Gets or sets the correlation identifier.
        /// </summary>
        /// <value>
        /// The correlation identifier.
        /// </value>
        public Guid CorrelationId { get; set; }
        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        public QueueStatuses Status { get; set; }
        /// <summary>
        /// Gets or sets the queued date time.
        /// </summary>
        /// <value>
        /// The queued date time.
        /// </value>
        public DateTime QueuedDateTime { get; set; }
        /// <summary>
        /// Gets or sets the queue process time.
        /// </summary>
        /// <value>
        /// The queue process time.
        /// </value>
        public DateTime? QueueProcessTime { get; set; }
        /// <summary>
        /// Gets or sets the heart beat.
        /// </summary>
        /// <value>
        /// The heart beat.
        /// </value>
        public DateTime? HeartBeat { get; set; }
        /// <summary>
        /// Gets or sets the expiration time.
        /// </summary>
        /// <value>
        /// The expiration time.
        /// </value>
        public DateTime? ExpirationTime { get; set; }

        /// <summary>
        /// Gets or sets the route.
        /// </summary>
        /// <value>
        /// The route.
        /// </value>
        public string Route { get; set; }

        /// <summary>
        /// Gets or sets the last exception.
        /// </summary>
        /// <value>
        /// The last exception.
        /// </value>
        public string LastException { get; set; }

        /// <summary>
        /// Gets or sets the last exception date.
        /// </summary>
        /// <value>
        /// The last exception date.
        /// </value>
        public DateTime LastExceptionDate { get; set; }
    }
}
