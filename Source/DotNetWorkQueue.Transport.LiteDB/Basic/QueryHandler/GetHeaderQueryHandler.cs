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
using System.Collections.Generic;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;
using LiteDB;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.QueryHandler
{
    /// <summary>
    /// Obtains a header
    /// </summary>
    public class GetHeaderQueryHandler : IQueryHandler<GetHeaderQuery<int>, IDictionary<string, object>>
    {
        private readonly ICompositeSerialization _serialization;
        private readonly IConnectionInformation _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetHeaderQueryHandler"/> class.
        /// </summary>
        /// <param name="serialization">The serialization.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        public GetHeaderQueryHandler(ICompositeSerialization serialization,
            IConnectionInformation connectionInformation,
            TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => serialization, serialization);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
            _serialization = serialization;
        }

        /// <inheritdoc />
        public IDictionary<string, object> Handle(GetHeaderQuery<int> query)
        {
            using (var db = new LiteDatabase(_connectionInformation.ConnectionString))
            {
                var queue = db.GetCollection<Schema.QueueTable>(_tableNameHelper.QueueName);

                var queueRecord = queue.FindById(query.Id);
                if (queueRecord != null)
                {
                    var headers =
                        _serialization.InternalSerializer.ConvertBytesTo<IDictionary<string, object>>(queueRecord
                            .Headers);
                    return headers;
                }
            }
            return null;
        }
    }
}
