using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;

namespace DotNetWorkQueue.Transport.RelationalDatabase
{
    public interface IPrepareCommandHandler<in TCommand>
    {
        void Handle(TCommand command, IDbCommand dbCommand, CommandStringTypes commandType);
    }
}
