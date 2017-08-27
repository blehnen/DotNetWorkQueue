using System.Data;
namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command
{
    public class SetStatusTableStatusTransactionCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SetStatusTableStatusTransactionCommand" /> class.
        /// </summary>
        /// <param name="queueId">The queue identifier.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="status">The status.</param>
        /// <param name="transaction">The transaction.</param>
        public SetStatusTableStatusTransactionCommand(long queueId,
            IDbConnection connection,
            QueueStatuses status,
            IDbTransaction transaction)
        {
            QueueId = queueId;
            Transaction = transaction;
            Connection = connection;
            Status = status;
        }
        /// <summary>
        /// Gets or sets the queue identifier.
        /// </summary>
        /// <value>
        /// The queue identifier.
        /// </value>
        public long QueueId { get; }

        public QueueStatuses Status { get; }

        public IDbConnection Connection { get; }

        public IDbTransaction Transaction { get; }
    }
}
