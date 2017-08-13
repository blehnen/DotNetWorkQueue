using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Decorator
{
    /// <summary>
    /// 
    /// </summary>
    public class DeleteMessageCommandDecorator : ICommandHandlerWithOutput<DeleteMessageCommand, long>
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly ICommandHandlerWithOutput<DeleteMessageCommand, long> _decorated;
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessageCommandDecorator" /> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="decorated">The decorated.</param>
        public DeleteMessageCommandDecorator(IConnectionInformation connectionInformation,
            ICommandHandlerWithOutput<DeleteMessageCommand, long> decorated)
        {
            Guard.NotNull(() => decorated, decorated);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            _connectionInformation = connectionInformation;
            _decorated = decorated;
        }
        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        public long Handle(DeleteMessageCommand command)
        {
            return !DatabaseExists.Exists(_connectionInformation.ConnectionString) ? 0 : _decorated.Handle(command);
        }
    }
}
