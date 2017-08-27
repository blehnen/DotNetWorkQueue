using System;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler
{
    public class SetStatusTableStatusTransactionCommandHandler: ICommandHandler<SetStatusTableStatusTransactionCommand>
    {
        private readonly IPrepareCommandHandler<SetStatusTableStatusTransactionCommand> _prepareCommand;

        public SetStatusTableStatusTransactionCommandHandler(
            IPrepareCommandHandler<SetStatusTableStatusTransactionCommand> prepareCommand)
        {
            Guard.NotNull(() => prepareCommand, prepareCommand);
            _prepareCommand = prepareCommand;
        }
        public void Handle(SetStatusTableStatusTransactionCommand command)
        {
            using (
                var commandSqlUpdateStatusRecord = command.Connection.CreateCommand())
            {
                commandSqlUpdateStatusRecord.Transaction = command.Transaction;
                _prepareCommand.Handle(command, commandSqlUpdateStatusRecord, CommandStringTypes.UpdateStatusRecord);
                commandSqlUpdateStatusRecord.ExecuteNonQuery();
            }
        }
    }
}
