namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic
{
    internal class ReceiveCommand
    {
        public ReceiveCommand(string commandText, ReceiveCommandTypes type)
        {
            CommandText = commandText;
            CommandType = type;
        }
        public string CommandText { get; }
        public ReceiveCommandTypes CommandType { get; }
    }

    internal enum ReceiveCommandTypes
    {
        Reader = 0,
        Update = 1
    }
}
