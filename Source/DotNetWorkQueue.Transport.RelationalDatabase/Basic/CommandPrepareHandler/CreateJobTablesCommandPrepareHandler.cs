using System.Data;
using System.Linq;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandPrepareHandler
{
    public class CreateJobTablesCommandPrepareHandler : IPrepareCommandHandler<CreateJobTablesCommand<ITable>>
    {
        public void Handle(CreateJobTablesCommand<ITable> command, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = command.Tables.Aggregate(string.Empty, (current, table) => current + (table.Script() + System.Environment.NewLine));
        }
    }
}
