using System.Data;
namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Transport.RelationalDatabase.ITransactionFactory" />
    public class TransactionFactory : ITransactionFactory
    {
        /// <summary>
        /// Creates a new instance of <seealso cref="T:DotNetWorkQueue.Transport.RelationalDatabase.ITransactionWrapper" />
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public ITransactionWrapper Create(IDbConnection connection)
        {
            return new TransactionWrapper(connection);
        }
    }
}
