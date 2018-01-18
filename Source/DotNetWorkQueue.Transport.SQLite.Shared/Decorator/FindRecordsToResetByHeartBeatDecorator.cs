using System.Collections.Generic;
using System.Linq;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Decorator
{
    /// <summary>
    /// 
    /// </summary>
    public class FindRecordsToResetByHeartBeatDecorator : IQueryHandler<FindMessagesToResetByHeartBeatQuery, IEnumerable<MessageToReset>>
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly IQueryHandler<FindMessagesToResetByHeartBeatQuery, IEnumerable<MessageToReset>> _decorated;
        private readonly DatabaseExists _databaseExists;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessageCommandDecorator" /> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="decorated">The decorated.</param>
        /// <param name="databaseExists">The database exists.</param>
        public FindRecordsToResetByHeartBeatDecorator(IConnectionInformation connectionInformation,
            IQueryHandler<FindMessagesToResetByHeartBeatQuery, IEnumerable<MessageToReset>> decorated,
            DatabaseExists databaseExists)
        {
            Guard.NotNull(() => decorated, decorated);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => databaseExists, databaseExists);
            _connectionInformation = connectionInformation;
            _decorated = decorated;
            _databaseExists = databaseExists;
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public IEnumerable<MessageToReset> Handle(FindMessagesToResetByHeartBeatQuery query)
        {
            return !_databaseExists.Exists(_connectionInformation.ConnectionString) ? Enumerable.Empty<MessageToReset>() : _decorated.Handle(query);
        }
    }
}
