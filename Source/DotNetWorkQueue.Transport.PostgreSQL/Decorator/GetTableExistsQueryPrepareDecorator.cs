using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.PostgreSQL.Decorator
{
    /// <inheritdoc />
    public class GetTableExistsQueryPrepareDecorator : IPrepareQueryHandler<GetTableExistsQuery, bool>
    {
        private readonly IPrepareQueryHandler<GetTableExistsQuery, bool> _decorated;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateJobTablesCommandDecorator"/> class.
        /// </summary>
        /// <param name="decorated">The decorated.</param>
        public GetTableExistsQueryPrepareDecorator(
            IPrepareQueryHandler<GetTableExistsQuery, bool> decorated)
        {
            Guard.NotNull(() => decorated, decorated);
            _decorated = decorated;
        }

        /// <inheritdoc />
        public void Handle(GetTableExistsQuery query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            //table name needs to be lower case
            _decorated.Handle(new GetTableExistsQuery(query.ConnectionString, query.TableName.ToLowerInvariant()), dbCommand, commandType);
        }
    }
}
