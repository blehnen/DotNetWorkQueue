using System.Collections.Generic;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Decorator
{
    /// <summary>
    /// 
    /// </summary>
    public class GetColumnNamesFromTableDecorator : IQueryHandler<GetColumnNamesFromTableQuery, List<string>>
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly IQueryHandler<GetColumnNamesFromTableQuery, List<string>> _decorated;
        private readonly DatabaseExists _databaseExists;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetColumnNamesFromTableDecorator" /> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="decorated">The decorated.</param>
        /// <param name="databaseExists">The database exists.</param>
        public GetColumnNamesFromTableDecorator(IConnectionInformation connectionInformation,
            IQueryHandler<GetColumnNamesFromTableQuery, List<string>> decorated,
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
        public List<string> Handle(GetColumnNamesFromTableQuery query)
        {
            return !_databaseExists.Exists(_connectionInformation.ConnectionString) ? new List<string>(0) : _decorated.Handle(query);
        }
    }
}
