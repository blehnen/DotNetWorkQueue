using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Decorator
{
    /// <inheritdoc />
    public class SetStatusTableStatusCommandDecorator : ICommandHandler<DeleteStatusTableStatusCommand>
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly ICommandHandler<DeleteStatusTableStatusCommand> _decorated;
        private readonly DatabaseExists _databaseExists;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessageCommandDecorator" /> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="decorated">The decorated.</param>
        /// <param name="databaseExists">The database exists.</param>
        public SetStatusTableStatusCommandDecorator(IConnectionInformation connectionInformation,
            ICommandHandler<DeleteStatusTableStatusCommand> decorated,
            DatabaseExists databaseExists)
        {
            Guard.NotNull(() => decorated, decorated);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => databaseExists, databaseExists);

            _connectionInformation = connectionInformation;
            _decorated = decorated;
            _databaseExists = databaseExists;
        }
        /// <inheritdoc />
        public void Handle(DeleteStatusTableStatusCommand command)
        {
            if (!_databaseExists.Exists(_connectionInformation.ConnectionString))
            {
                return;
            }
            _decorated.Handle(command);
        }
    }
}
