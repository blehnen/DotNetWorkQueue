using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandPrepareHandler
{
    public class DeleteTableCommandPrepareHandler : IPrepareCommandHandler<DeleteTableCommand>
    {
        private readonly CommandStringCache _commandCache;
        public DeleteTableCommandPrepareHandler(CommandStringCache commandCache)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
        }
        public void Handle(DeleteTableCommand command, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText =
                _commandCache.GetCommand(commandType, command.Table);
        }
    }
}
