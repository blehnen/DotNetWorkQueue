using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandPrepareHandler
{
    /// <inheritdoc />
    public class DeleteTableCommandPrepareHandler: IPrepareCommandHandler<DeleteTableCommand>
    {
        private readonly CommandStringCache _commandCache;
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteTableCommandPrepareHandler"/> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        public DeleteTableCommandPrepareHandler(CommandStringCache commandCache)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
        }

        /// <inheritdoc />
        public void Handle(DeleteTableCommand command, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText =
                _commandCache.GetCommand(commandType, command.Table.ToLowerInvariant());
        }
    }
}
