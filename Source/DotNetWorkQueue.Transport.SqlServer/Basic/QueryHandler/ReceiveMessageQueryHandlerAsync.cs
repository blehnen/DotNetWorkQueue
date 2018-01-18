using System.Data.SqlClient;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler
{
    /// <summary>
    /// Dequeues a message.
    /// </summary>
    internal class ReceiveMessageQueryHandlerAsync : IQueryHandler<ReceiveMessageQueryAsync<SqlConnection, SqlTransaction>, Task<IReceivedMessageInternal>>
    {
        private readonly BuildDequeueCommand _buildDequeueCommand;
        private readonly ReadMessage _readMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler.ReceiveMessageQueryHandler" /> class.
        /// </summary>
        /// <param name="readMessage">The read message.</param>
        /// <param name="buildDequeueCommand">The build dequeue command.</param>
        public ReceiveMessageQueryHandlerAsync(ReadMessage readMessage,
            BuildDequeueCommand buildDequeueCommand)
        {
            Guard.NotNull(() => readMessage, readMessage);
            Guard.NotNull(() => buildDequeueCommand, buildDequeueCommand);

            _readMessage = readMessage;
            _buildDequeueCommand = buildDequeueCommand;
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public async Task<IReceivedMessageInternal> Handle(ReceiveMessageQueryAsync<SqlConnection, SqlTransaction> query)
        {
            using (var selectCommand = query.Connection.CreateCommand())
            {
                _buildDequeueCommand.BuildCommand(selectCommand, query);
                using (var reader = await selectCommand.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    return _readMessage.Read(reader);
                }
            }
        }
    }
}
