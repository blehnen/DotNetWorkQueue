using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Decorator
{
    /// <summary>
    /// 
    /// </summary>
    public class ResetHeartBeatCommandDecorator: ICommandHandlerWithOutput<ResetHeartBeatCommand, long>
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly ICommandHandlerWithOutput<ResetHeartBeatCommand, long> _decorated;
        private readonly DatabaseExists _databaseExists;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessageCommandDecorator" /> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="decorated">The decorated.</param>
        /// <param name="databaseExists">The database exists.</param>
        public ResetHeartBeatCommandDecorator(IConnectionInformation connectionInformation,
            ICommandHandlerWithOutput<ResetHeartBeatCommand, long> decorated,
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
        /// <returns></returns>
        public long Handle(ResetHeartBeatCommand command)
        {
            return !_databaseExists.Exists(_connectionInformation.ConnectionString) ? 0 : _decorated.Handle(command);
        }
    }
}
