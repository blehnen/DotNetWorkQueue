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
using System.Data.SQLite;
using DotNetWorkQueue.Transport.SQLite.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic.QueryHandler
{
    /// <summary>
    /// Gets the queue options
    /// </summary>
    internal class GetQueueOptionsQueryHandler : IQueryHandler<GetQueueOptionsQuery, SqLiteMessageQueueTransportOptions>
    {
        private readonly IInternalSerializer _serializer;
        private readonly IQueryHandler<GetTableExistsQuery, bool> _tableExists;
        private readonly IConnectionInformation _connectionInformation;
        private readonly SqLiteCommandStringCache _commandCache;
        private readonly TableNameHelper _tableNameHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetQueueOptionsQueryHandler" /> class.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="tableExists">The table exists.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        public GetQueueOptionsQueryHandler(IInternalSerializer serializer, 
            IQueryHandler<GetTableExistsQuery, bool> tableExists,
            IConnectionInformation connectionInformation,
            SqLiteCommandStringCache commandCache,
            TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => serializer, serializer);
            Guard.NotNull(() => tableExists, tableExists);
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _serializer = serializer;
            _tableExists = tableExists;
            _connectionInformation = connectionInformation;
            _commandCache = commandCache;
            _tableNameHelper = tableNameHelper;
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        public SqLiteMessageQueueTransportOptions Handle(GetQueueOptionsQuery query)
        {
            if (!_tableExists.Handle(new GetTableExistsQuery(_connectionInformation.ConnectionString,
                _tableNameHelper.ConfigurationName))) return null;

            using (var conn = new SQLiteConnection(_connectionInformation.ConnectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = _commandCache.GetCommand(SqLiteCommandStringTypes.GetConfiguration);
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read()) return null;
                        var options =
                            _serializer.ConvertBytesTo<SqLiteMessageQueueTransportOptions>((byte[]) reader[0]);
                        return options;
                    }
                }
            }
        }
    }
}
