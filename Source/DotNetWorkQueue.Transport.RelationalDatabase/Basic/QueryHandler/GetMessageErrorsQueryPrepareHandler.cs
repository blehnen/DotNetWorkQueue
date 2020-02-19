using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    internal class GetMessageErrorsQueryPrepareHandler : IPrepareQueryHandler<GetMessageErrorsQuery, Dictionary<string, int>>
    {
        private readonly CommandStringCache _commandCache;
        /// <summary>
        /// Initializes a new instance of the <see cref="GetMessageErrorsQueryPrepareHandler"/> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        public GetMessageErrorsQueryPrepareHandler(CommandStringCache commandCache)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
        }

        /// <inheritdoc />
        public void Handle(GetMessageErrorsQuery query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = _commandCache.GetCommand(commandType);

            var queueid = dbCommand.CreateParameter();
            queueid.ParameterName = "@QueueID";
            queueid.DbType = DbType.Int64;
            queueid.Value = query.QueueId;
            dbCommand.Parameters.Add(queueid);
        }
    }
}
