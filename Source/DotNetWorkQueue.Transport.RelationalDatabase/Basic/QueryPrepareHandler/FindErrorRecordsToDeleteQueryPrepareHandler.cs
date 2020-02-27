using System.Collections.Generic;
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler
{
    public class FindErrorRecordsToDeleteQueryPrepareHandler : IPrepareQueryHandler<FindErrorMessagesToDeleteQuery, IEnumerable<long>>
    {
        private readonly CommandStringCache _commandCache;
        private readonly IMessageErrorConfiguration _configuration;
        private readonly IGetTime _getTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindErrorRecordsToDeleteQueryPrepareHandler"/> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="timeFactory">The time factory.</param>
        /// <param name="configuration">The configuration.</param>
        public FindErrorRecordsToDeleteQueryPrepareHandler(CommandStringCache commandCache,
            IGetTimeFactory timeFactory,
            IMessageErrorConfiguration configuration)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => timeFactory, timeFactory);
            _commandCache = commandCache;
            _configuration = configuration;
            _getTime = timeFactory.Create();
        }

        /// <inheritdoc />
        public void Handle(FindErrorMessagesToDeleteQuery query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = _commandCache.GetCommand(commandType);

            var param = dbCommand.CreateParameter();
            param.ParameterName = "@CurrentDate";
            param.DbType = DbType.DateTime;
            param.Value = _getTime.GetCurrentUtcDate().Add(_configuration.MessageAge);
            dbCommand.Parameters.Add(param);
        }
    }
}
