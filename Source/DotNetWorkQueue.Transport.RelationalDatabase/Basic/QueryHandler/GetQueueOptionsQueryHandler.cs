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
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    /// <inheritdoc />
    /// <summary>
    /// Gets the queue options
    /// </summary>
    public class GetQueueOptionsQueryHandler<TTransportOptions> : IQueryHandler<GetQueueOptionsQuery<TTransportOptions>, TTransportOptions>
        where TTransportOptions: class, ITransportOptions
    {
        private readonly IInternalSerializer _serializer;
        private readonly IQueryHandler<GetTableExistsQuery, bool> _tableExists;
        private readonly IConnectionInformation _connectionInformation;
        private readonly IPrepareQueryHandler<GetQueueOptionsQuery<TTransportOptions>, TTransportOptions> _prepareQuery;
        private readonly TableNameHelper _tableNameHelper;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IReadColumn _readColumn;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetQueueOptionsQueryHandler{TTransportOptions}" /> class.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="tableExists">The table exists.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="prepareQuery">The prepare query.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="readColumn">The read column.</param>
        public GetQueueOptionsQueryHandler(IInternalSerializer serializer,
                    IQueryHandler<GetTableExistsQuery, bool> tableExists,
                    IConnectionInformation connectionInformation,
                    IPrepareQueryHandler<GetQueueOptionsQuery<TTransportOptions>, TTransportOptions> prepareQuery,
                    TableNameHelper tableNameHelper,
                    IDbConnectionFactory dbConnectionFactory,
                    IReadColumn readColumn)
        {
            Guard.NotNull(() => serializer, serializer);
            Guard.NotNull(() => tableExists, tableExists);
            Guard.NotNull(() => prepareQuery, prepareQuery);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            Guard.NotNull(() => readColumn, readColumn);

            _serializer = serializer;
            _tableExists = tableExists;
            _connectionInformation = connectionInformation;
            _prepareQuery = prepareQuery;
            _tableNameHelper = tableNameHelper;
            _dbConnectionFactory = dbConnectionFactory;
            _readColumn = readColumn;
        }
        /// <inheritdoc />
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        public TTransportOptions Handle(GetQueueOptionsQuery<TTransportOptions> query)
        {
            if (!_tableExists.Handle(new GetTableExistsQuery(_connectionInformation.ConnectionString,
                _tableNameHelper.ConfigurationName))) return null;

            using (var conn = _dbConnectionFactory.Create())
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    _prepareQuery.Handle(query, command, CommandStringTypes.GetConfiguration);
                    using (var reader = command.ExecuteReader())
                    {
                        return !reader.Read() ? null : _serializer.ConvertBytesTo<TTransportOptions>(_readColumn.ReadAsByteArray(CommandStringTypes.GetConfiguration, 0, reader));
                    }
                }
            }
        }
    }
}
