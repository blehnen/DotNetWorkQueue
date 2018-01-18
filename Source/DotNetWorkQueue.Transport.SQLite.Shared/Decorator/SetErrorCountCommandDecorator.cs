using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Decorator
{
    /// <summary>
    /// 
    /// </summary>
    public class SetErrorCountCommandDecorator : ICommandHandler<SetErrorCountCommand>
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly ICommandHandler<SetErrorCountCommand> _decorated;
        private readonly DatabaseExists _databaseExists;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessageCommandDecorator" /> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="decorated">The decorated.</param>
        /// <param name="databaseExists">The database exists.</param>
        public SetErrorCountCommandDecorator(IConnectionInformation connectionInformation,
            ICommandHandler<SetErrorCountCommand> decorated,
            DatabaseExists databaseExists)
        {
            Guard.NotNull(() => decorated, decorated);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => databaseExists, databaseExists);

            _connectionInformation = connectionInformation;
            _decorated = decorated;
            _databaseExists = databaseExists;
        }
        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        public void Handle(SetErrorCountCommand command)
        {
            if (!_databaseExists.Exists(_connectionInformation.ConnectionString))
            {
                return;
            }
            _decorated.Handle(command);
        }
    }
}
