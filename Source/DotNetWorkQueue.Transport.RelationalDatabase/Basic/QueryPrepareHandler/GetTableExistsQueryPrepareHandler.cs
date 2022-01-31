// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler
{
    /// <inheritdoc />
    public class GetTableExistsQueryPrepareHandler : IPrepareQueryHandler<GetTableExistsQuery, bool>
    {
        private readonly CommandStringCache _commandCache;
        private readonly IConnectionInformation _connectionInformation;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetTableExistsQueryPrepareHandler"/> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public GetTableExistsQueryPrepareHandler(CommandStringCache commandCache,
            IConnectionInformation connectionInformation)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            _commandCache = commandCache;
            _connectionInformation = connectionInformation;
        }

        /// <inheritdoc />
        public void Handle(GetTableExistsQuery query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = _commandCache.GetCommand(commandType);

            var parameter = dbCommand.CreateParameter();
            parameter.ParameterName = "@Table";
            parameter.DbType = DbType.AnsiString;
            parameter.Value = query.TableName;
            dbCommand.Parameters.Add(parameter);

            var parameterDb = dbCommand.CreateParameter();
            parameterDb.ParameterName = "@Database";
            parameterDb.DbType = DbType.AnsiString;
            parameterDb.Value = _connectionInformation.Container;
            dbCommand.Parameters.Add(parameterDb);
        }
    }
}
