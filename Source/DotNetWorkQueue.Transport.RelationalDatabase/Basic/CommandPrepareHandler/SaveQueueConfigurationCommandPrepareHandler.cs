using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandPrepareHandler
{
    public class SaveQueueConfigurationCommandPrepareHandler: IPrepareCommandHandler<SaveQueueConfigurationCommand>
    {
        private readonly CommandStringCache _commandCache;

        public SaveQueueConfigurationCommandPrepareHandler(CommandStringCache commandCache)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
        }
        public void Handle(SaveQueueConfigurationCommand command, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = _commandCache.GetCommand(CommandStringTypes.SaveConfiguration);
            var param = dbCommand.CreateParameter();
            param.ParameterName = "@Configuration";
            param.DbType = DbType.Binary;
            param.Value = command.Configuration;
            dbCommand.Parameters.Add(param);
        }
    }
}
