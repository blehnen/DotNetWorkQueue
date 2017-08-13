// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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

using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.QueryHandler
{
    /// <summary>
    /// Gets the queue options
    /// </summary>
    internal class GetQueueOptionsQueryHandler : IQueryHandler<GetQueueOptionsQuery<PostgreSqlMessageQueueTransportOptions>, PostgreSqlMessageQueueTransportOptions>
    {
        private readonly IInternalSerializer _serializer;
        private readonly IQueryHandler<GetTableExistsQuery, bool> _tableExists;
        private readonly IConnectionInformation _connectionInformation;
        private readonly PostgreSqlCommandStringCache _commandCache;
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
            PostgreSqlCommandStringCache commandCache,
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
        public PostgreSqlMessageQueueTransportOptions Handle(GetQueueOptionsQuery<PostgreSqlMessageQueueTransportOptions> query)
        {
            if (!_tableExists.Handle(new GetTableExistsQuery(_connectionInformation.ConnectionString,
                _tableNameHelper.ConfigurationName))) return null;

            using (var conn = new NpgsqlConnection(_connectionInformation.ConnectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = _commandCache.GetCommand(CommandStringTypes.GetConfiguration);
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read()) return null;
                        var options =
                            _serializer.ConvertBytesTo<PostgreSqlMessageQueueTransportOptions>((byte[]) reader[0]);
                        return options;
                    }
                }
            }
        }
    }
}
