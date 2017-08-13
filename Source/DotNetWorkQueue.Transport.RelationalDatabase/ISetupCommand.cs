using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;

namespace DotNetWorkQueue.Transport.RelationalDatabase
{
    public interface ISetupCommand
    {
        void Setup(IDbCommand command, CommandStringTypes type, object commandParams);
    }
}
