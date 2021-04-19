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
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.QueryPrepareHandler
{
    /// <inheritdoc />
    /// <summary>
    /// Prepares to run the get table exists query
    /// </summary>
    public class GetTableExistsTransactionQueryPrepareHandler : IPrepareQueryHandler<GetTableExistsTransactionQuery, bool>
    {
        private readonly CommandStringCache _commandCache;
        private readonly IConnectionInformation _connectionInformation;
        private readonly ISqlSchema _schema;

        /// <summary>Initializes a new instance of the <see cref="GetTableExistsTransactionQueryPrepareHandler"/> class.</summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="schema">The schema that the queue is using</param>
        public GetTableExistsTransactionQueryPrepareHandler(CommandStringCache commandCache,
            IConnectionInformation connectionInformation, ISqlSchema schema)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
            _connectionInformation = connectionInformation;
            _schema = schema;
        }

        /// <inheritdoc />
        public void Handle(GetTableExistsTransactionQuery query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = _commandCache.GetCommand(commandType);
            var tableName = query.TableName;

            var parameter = dbCommand.CreateParameter();
            parameter.ParameterName = "@Table";
            parameter.DbType = DbType.AnsiString;

            if (tableName.StartsWith(_schema.Schema))
                tableName = tableName.Replace($"{_schema.Schema}.", string.Empty);

            parameter.Value = tableName;
            dbCommand.Parameters.Add(parameter);

            var parameterDb = dbCommand.CreateParameter();
            parameterDb.ParameterName = "@Database";
            parameterDb.DbType = DbType.AnsiString;
            parameterDb.Value = _connectionInformation.Container;
            dbCommand.Parameters.Add(parameterDb);
        }
    }
}
