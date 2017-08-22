using System.Data;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query
{
    public class GetTableExistsTransactionQuery : IQuery<bool>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetTableExistsQuery" /> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="trans">The trans.</param>
        /// <param name="tableName">Name of the table.</param>
        public GetTableExistsTransactionQuery(IDbConnection connection, IDbTransaction trans, string tableName)
        {
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => trans, trans);
            Guard.NotNullOrEmpty(() => tableName, tableName);

            Connection = connection;
            Trans = trans;
            TableName = tableName;
        }

        public IDbConnection Connection { get; }
        public IDbTransaction Trans { get; }
        public string TableName { get; }
    }
}
