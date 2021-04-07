// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
    /// Meta data table
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Transport.LiteDb.Basic.ITable" />
    public class MetaDataTable: ITable
    {
        /// <inheritdoc />
        public bool Create(IConnectionInformation connection, LiteDbMessageQueueTransportOptions options, TableNameHelper helper)
        {
            using (var db = new LiteDatabase(connection.ConnectionString))
            {
                var col = db.GetCollection<MetaDataTable>(helper.MetaDataName);

                col.EnsureIndex(x => x.Id);
                col.EnsureIndex(x => x.QueueId, true);

                if(options.EnableStatus)
                    col.EnsureIndex(x => x.Status);
                if(options.EnableMessageExpiration)
                    col.EnsureIndex(x => x.ExpirationTime);
                if(options.EnableHeartBeat)
                    col.EnsureIndex(x => x.HeartBeat);
                if(options.EnableRoute)
                    col.EnsureIndex(x => x.Route);

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

    }
}
