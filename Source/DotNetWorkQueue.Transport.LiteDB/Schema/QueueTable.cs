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
using DotNetWorkQueue.Transport.LiteDb.Basic;
using LiteDB;

namespace DotNetWorkQueue.Transport.LiteDb.Schema
{
    /// <summary>
    /// Queue table
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Transport.LiteDb.Basic.ITable" />
    public class QueueTable: ITable
    {

        /// <inheritdoc />
        public bool Create(IConnectionInformation connection, LiteDbMessageQueueTransportOptions options, TableNameHelper helper)
        {
            using (var db = new LiteDatabase(connection.ConnectionString))
            {
                var col = db.GetCollection<QueueTable>(helper.QueueName);

                //indexed by Id
                col.EnsureIndex(x => x.Id);

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
        /// Gets or sets the body.
        /// </summary>
        /// <value>
        /// The body.
        /// </value>
        public byte[] Body { get; set; }
        /// <summary>
        /// Gets or sets the headers.
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        public byte[] Headers { get; set; }
    }
}
