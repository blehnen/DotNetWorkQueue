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

using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    /// <summary>
    /// Determines if a specific table exists in the schema
    /// </summary>
    internal class GetTableExistsQueryHandler : IQueryHandler<GetTableExistsQuery, bool>
    {
        private readonly CommandStringCache _commandCache;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IConnectionInformation _connectionInformation;
        private readonly ICaseTableName _caseTableName;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetTableExistsQueryHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="caseTableName">Name of the case table.</param>
        public GetTableExistsQueryHandler(CommandStringCache commandCache,
            IDbConnectionFactory dbConnectionFactory,
            IConnectionInformation connectionInformation,
            ICaseTableName caseTableName)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            _commandCache = commandCache;
            _dbConnectionFactory = dbConnectionFactory;
            _connectionInformation = connectionInformation;
            _caseTableName = caseTableName;
        }

        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        public bool Handle(GetTableExistsQuery query)
        {
            using (var conn = _dbConnectionFactory.Create())
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = _commandCache.GetCommand(CommandStringTypes.GetTableExists);

                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@Table";
                    parameter.DbType = DbType.AnsiString;
                    parameter.Value = _caseTableName.FormatTableName(query.TableName);
                    command.Parameters.Add(parameter);

                    var parameterDb = command.CreateParameter();
                    parameterDb.ParameterName = "@Database";
                    parameterDb.DbType = DbType.AnsiString;
                    parameterDb.Value = _connectionInformation.Container;
                    command.Parameters.Add(parameterDb);

                    using (var reader = command.ExecuteReader())
                    {
                        var result = reader.Read();
                        return result;
                    }
                }
            }
        }
    }
}
