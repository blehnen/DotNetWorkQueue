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
using DotNetWorkQueue.Transport.LiteDb.Basic.Query;
using DotNetWorkQueue.Transport.LiteDb.Schema;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;
using LiteDB;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.QueryHandler
{
    /// <summary>
    /// Gets the queue options
    /// </summary>
    public class GetQueueOptionsQueryHandler<TTransportOptions> : IQueryHandler<GetQueueOptionsQuery<TTransportOptions>, TTransportOptions>
        where TTransportOptions: class
    {
        private readonly IInternalSerializer _serializer;
        private readonly IQueryHandler<GetTableExistsQuery, bool> _tableExists;
        private readonly IConnectionInformation _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetQueueOptionsQueryHandler{TTransportOptions}" /> class.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="tableExists">The table exists.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        public GetQueueOptionsQueryHandler(IInternalSerializer serializer,
                    IQueryHandler<GetTableExistsQuery, bool> tableExists,
                    IConnectionInformation connectionInformation,
                    TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => serializer, serializer);
            Guard.NotNull(() => tableExists, tableExists);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _serializer = serializer;
            _tableExists = tableExists;
            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
        }
        /// <inheritdoc />
        public TTransportOptions Handle(GetQueueOptionsQuery<TTransportOptions> query)
        {
            using (var db = new LiteDatabase(_connectionInformation.ConnectionString))
            {
                if (!_tableExists.Handle(new GetTableExistsQuery(db,
                    _tableNameHelper.ConfigurationName))) return null;

                var col = db.GetCollection<ConfigurationTable>(_tableNameHelper.ConfigurationName);
                var result = col.FindOne(global::LiteDB.Query.All());
                return result?.Configuration != null ? _serializer.ConvertBytesTo<TTransportOptions>(result.Configuration) : null;
            }
        }
    }
}
