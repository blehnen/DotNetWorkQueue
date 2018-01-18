using System.Collections.Generic;
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.PostgreSQL.Decorator
{
    /// <inheritdoc />
    public class
        GetColumnNamesFromTableQueryPrepareDecorator : IPrepareQueryHandler<GetColumnNamesFromTableQuery, List<string>>
    {
        private readonly IPrepareQueryHandler<GetColumnNamesFromTableQuery, List<string>> _decorated;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateJobTablesCommandDecorator"/> class.
        /// </summary>
        /// <param name="decorated">The decorated.</param>
        public GetColumnNamesFromTableQueryPrepareDecorator(
            IPrepareQueryHandler<GetColumnNamesFromTableQuery, List<string>> decorated)
        {
            Guard.NotNull(() => decorated, decorated);
            _decorated = decorated;
        }

        /// <inheritdoc />
        public void Handle(GetColumnNamesFromTableQuery query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            //table name needs to be lower case
            _decorated.Handle(new GetColumnNamesFromTableQuery(query.ConnectionString, query.TableName.ToLowerInvariant()), dbCommand,
                commandType);
        }
    }
}
