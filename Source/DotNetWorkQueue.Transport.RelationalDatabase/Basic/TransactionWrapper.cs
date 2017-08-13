using System.Data;
using DotNetWorkQueue.Validation;
namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Transport.RelationalDatabase.ITransactionWrapper" />
    public class TransactionWrapper: ITransactionWrapper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionWrapper"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public TransactionWrapper(IDbConnection connection)
        {
            Guard.NotNull(() => connection, connection);
            Connection = connection;
        }
        /// <summary>
        /// Gets or sets the connection.
        /// </summary>
        /// <value>
        /// The connection.
        /// </value>
        public IDbConnection Connection { get; set; }
        /// <summary>
        /// Begins the transaction.
        /// </summary>
        /// <returns></returns>
        public IDbTransaction BeginTransaction()
        {
            return Connection.BeginTransaction();
        }
    }
}
