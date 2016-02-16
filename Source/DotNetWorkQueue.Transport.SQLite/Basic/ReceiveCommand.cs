using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    internal class ReceiveCommand
    {
        public ReceiveCommand(string commandText, ReceiveCommandTypes type)
        {
            CommandText = commandText;
            CommandType = type;
        }
        public string CommandText { get; private set; }
        public ReceiveCommandTypes CommandType { get; private set; }
    }

    internal enum ReceiveCommandTypes
    {
        Reader = 0,
        Update = 1
    }
}
