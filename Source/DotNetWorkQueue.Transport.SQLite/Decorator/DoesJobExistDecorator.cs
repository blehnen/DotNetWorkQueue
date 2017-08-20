using System;
using System.Data.SQLite;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Decorator
{
    /// <summary>
    /// 
    /// </summary>
    public class DoesJobExistDecorator : IQueryHandler<DoesJobExistQuery<SQLiteConnection, SQLiteTransaction>, QueueStatuses>
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly IQueryHandler<DoesJobExistQuery<SQLiteConnection, SQLiteTransaction>, QueueStatuses> _decorated;
        /// <summary>
        /// Initializes a new instance of the <see cref="GetColumnNamesFromTableDecorator" /> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="decorated">The decorated.</param>
        public DoesJobExistDecorator(IConnectionInformation connectionInformation,
            IQueryHandler<DoesJobExistQuery<SQLiteConnection, SQLiteTransaction>, QueueStatuses> decorated)
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
        public QueueStatuses Handle(DoesJobExistQuery<SQLiteConnection, SQLiteTransaction> query)
        {
            return !DatabaseExists.Exists(_connectionInformation.ConnectionString) ? QueueStatuses.NotQueued : _decorated.Handle(query);
        }
    }
}
