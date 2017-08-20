
using System.Collections.Generic;
using System.Linq;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Decorator
{
    /// <summary>
    /// Verifies that the local DB exists before running the query to find expired records
    /// </summary>
    public class FindExpiredRecordsToDeleteDecorator : IQueryHandler<FindExpiredMessagesToDeleteQuery, IEnumerable<long>>
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly IQueryHandler<FindExpiredMessagesToDeleteQuery, IEnumerable<long>> _decorated;
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessageCommandDecorator" /> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="decorated">The decorated.</param>
        public FindExpiredRecordsToDeleteDecorator(IConnectionInformation connectionInformation,
            IQueryHandler<FindExpiredMessagesToDeleteQuery, IEnumerable<long>> decorated)
        {
            Guard.NotNull(() => decorated, decorated);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            _connectionInformation = connectionInformation;
            _decorated = decorated;
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public IEnumerable<long> Handle(FindExpiredMessagesToDeleteQuery query)
        {
            return !DatabaseExists.Exists(_connectionInformation.ConnectionString) ? Enumerable.Empty<long>() : _decorated.Handle(query);
        }
    }
}
