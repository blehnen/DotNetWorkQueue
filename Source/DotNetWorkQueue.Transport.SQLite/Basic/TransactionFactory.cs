using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Transport.RelationalDatabase.ITransactionFactory" />
    internal class TransactionFactory : ITransactionFactory
    {
        private readonly ISqLiteTransactionFactory _factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionFactory"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        public TransactionFactory(ISqLiteTransactionFactory factory)
        {
            Guard.NotNull(() => factory, factory);
            _factory = factory;
        }
        /// <summary>
        /// Creates a new instance of <seealso cref="T:DotNetWorkQueue.Transport.RelationalDatabase.ITransactionWrapper" />
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public ITransactionWrapper Create(IDbConnection connection)
        {
            return _factory.Create(connection);
        }
    }
}
