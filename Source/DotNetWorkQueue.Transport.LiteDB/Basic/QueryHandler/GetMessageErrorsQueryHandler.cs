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
    internal class GetMessageErrorsQueryHandler : IQueryHandler<GetMessageErrorsQuery<int>, Dictionary<string, int>>
    {
        private readonly LiteDbConnectionManager _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetMessageErrorsQueryHandler"/> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        public GetMessageErrorsQueryHandler (LiteDbConnectionManager connectionInformation,
            TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
        }

        /// <inheritdoc />
        public Dictionary<string, int> Handle(GetMessageErrorsQuery<int> query)
        {
            var returnData = new Dictionary<string, int>();
            using (var db = _connectionInformation.GetDatabase())
            {
                var col = db.Database.GetCollection<Schema.ErrorTrackingTable>(_tableNameHelper.ErrorTrackingName);

                var results = col.Query()
                    .Where(x => x.QueueId.Equals(query.QueueId))
                    .ToList();

                foreach (var record in results)
                {
                    returnData.Add(record.ExceptionType, record.RetryCount);
                }

                return returnData;
            }
        }
    }
}
