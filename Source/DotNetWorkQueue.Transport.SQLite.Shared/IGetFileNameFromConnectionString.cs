using DotNetWorkQueue.Transport.SQLite.Shared.Basic;

namespace DotNetWorkQueue.Transport.SQLite.Shared
{
    public interface IGetFileNameFromConnectionString
    {
        ConnectionStringInfo GetFileName(string connectionString);
    }
}
