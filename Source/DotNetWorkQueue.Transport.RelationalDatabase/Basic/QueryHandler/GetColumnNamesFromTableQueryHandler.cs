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
using System.Collections.Generic;
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    /// <summary>
    /// Returns all standard column names for a specific table.
    /// </summary>
    internal class GetColumnNamesFromTableQueryHandler : IQueryHandler<GetColumnNamesFromTableQuery, List<string>>
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly CommandStringCache _commandStringCache;
        private readonly ICaseTableName _caseTableName;
        private readonly IReadColumn _readColumn;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetColumnNamesFromTableQueryHandler" /> class.
        /// </summary>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="commandStringCache">The command string cache.</param>
        /// <param name="caseTableName">Name of the case table.</param>
        /// <param name="readColumn">The read column.</param>
        public GetColumnNamesFromTableQueryHandler(IDbConnectionFactory dbConnectionFactory,
            CommandStringCache commandStringCache,
            ICaseTableName caseTableName,
            IReadColumn readColumn)
        {
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            Guard.NotNull(() => commandStringCache, commandStringCache);
            Guard.NotNull(() => caseTableName, caseTableName);
            Guard.NotNull(() => readColumn, readColumn);
            _dbConnectionFactory = dbConnectionFactory;
            _commandStringCache = commandStringCache;
            _caseTableName = caseTableName;
            _readColumn = readColumn;
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        public List<string> Handle(GetColumnNamesFromTableQuery query)
        {
            var columns = new List<string>();
            using (var connection = _dbConnectionFactory.Create())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        _commandStringCache.GetCommand(CommandStringTypes.GetColumnNamesFromTable, query.TableName);

                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@TableName";
                    parameter.DbType = DbType.AnsiString;
                    parameter.Value = _caseTableName.FormatTableName(query.TableName);
                    command.Parameters.Add(parameter);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            columns.Add(_readColumn.ReadAsString(CommandStringTypes.GetColumnNamesFromTable, reader));
                        }
                    }
                }
            }
            return columns;
        }
    }
}
