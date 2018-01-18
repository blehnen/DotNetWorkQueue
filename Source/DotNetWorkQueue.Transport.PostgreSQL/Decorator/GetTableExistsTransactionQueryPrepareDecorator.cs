using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.PostgreSQL.Decorator
{
    /// <inheritdoc />
    public class GetTableExistsTransactionQueryPrepareDecorator : IPrepareQueryHandler<GetTableExistsTransactionQuery, bool>
    {
        private readonly IPrepareQueryHandler<GetTableExistsTransactionQuery, bool> _decorated;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateJobTablesCommandDecorator"/> class.
        /// </summary>
        /// <param name="decorated">The decorated.</param>
        public GetTableExistsTransactionQueryPrepareDecorator(
            IPrepareQueryHandler<GetTableExistsTransactionQuery, bool> decorated)
        {
            Guard.NotNull(() => decorated, decorated);
            _decorated = decorated;
        }

        /// <inheritdoc />
        public void Handle(GetTableExistsTransactionQuery query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            //table name needs to be lower case
            _decorated.Handle(new GetTableExistsTransactionQuery(query.Connection,
                query.Trans, query.TableName.ToLowerInvariant()), dbCommand, commandType );
        }
    }
}
