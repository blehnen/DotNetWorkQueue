namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command
{
    public class DeleteTableCommand
    {
        public DeleteTableCommand(string table)
        {
            Table = table;
        }
        public string Table { get; }
    }
}
