using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Decorator
{
    /// <summary>
    /// Decorator for error record - returns false if the DB doesn't exist on the file system
    /// </summary>
    public class GetErrorRecordExistsQueryDecorator : IQueryHandler<GetErrorRecordExistsQuery, bool>
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly IQueryHandler<GetErrorRecordExistsQuery, bool> _decorated;
        private readonly DatabaseExists _databaseExists;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessageCommandDecorator" /> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="decorated">The decorated.</param>
        /// <param name="databaseExists">The database exists.</param>
        public GetErrorRecordExistsQueryDecorator(IConnectionInformation connectionInformation,
            IQueryHandler<GetErrorRecordExistsQuery, bool> decorated,
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
        public bool Handle(GetErrorRecordExistsQuery query)
        {
            return _databaseExists.Exists(_connectionInformation.ConnectionString) && _decorated.Handle(query);
        }
    }
}
