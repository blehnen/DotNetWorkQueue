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

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler
{
    public class GetErrorRetryCountQueryPrepareHandler : IPrepareQueryHandler<GetErrorRetryCountQuery, int>
    {
        private readonly CommandStringCache _commandCache;
        public GetErrorRetryCountQueryPrepareHandler(CommandStringCache commandCache)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
        }

        public void Handle(GetErrorRetryCountQuery query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = _commandCache.GetCommand(commandType);

            var queueid = dbCommand.CreateParameter();
            queueid.ParameterName = "@QueueID";
            queueid.DbType = DbType.Int64;
            queueid.Value = query.QueueId;
            dbCommand.Parameters.Add(queueid);

            var exceptionType = dbCommand.CreateParameter();
            exceptionType.ParameterName = "@ExceptionType";
            exceptionType.DbType = DbType.AnsiString;
            exceptionType.Size = 500;
            exceptionType.Value = query.ExceptionType;
            dbCommand.Parameters.Add(exceptionType);
        }
    }
}
