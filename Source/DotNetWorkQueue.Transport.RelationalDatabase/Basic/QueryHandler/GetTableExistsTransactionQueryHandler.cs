using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    public class GetTableExistsTransactionQueryHandler: IQueryHandler<GetTableExistsTransactionQuery, bool>
    {
        private readonly IPrepareQueryHandler<GetTableExistsTransactionQuery, bool> _prepareQuery;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetTableExistsQueryHandler" /> class.
        /// </summary>
        /// <param name="prepareQuery">The prepare query.</param>
        public GetTableExistsTransactionQueryHandler(IPrepareQueryHandler<GetTableExistsTransactionQuery, bool> prepareQuery)
        {
            Guard.NotNull(() => prepareQuery, prepareQuery);
            _prepareQuery = prepareQuery;
        }

        public bool Handle(GetTableExistsTransactionQuery query)
        {
            using (var command = query.Connection.CreateCommand())
            {
                command.Transaction = query.Trans;
                _prepareQuery.Handle(query, command, CommandStringTypes.GetTableExists);
                using (var reader = command.ExecuteReader())
                {
                    var result = reader.Read();
                    return result;
                }
            }
        }
    }
}
