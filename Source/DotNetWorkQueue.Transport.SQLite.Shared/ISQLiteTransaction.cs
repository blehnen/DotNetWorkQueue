using DotNetWorkQueue.Transport.RelationalDatabase;

namespace DotNetWorkQueue.Transport.SQLite.Shared
{
    /// <summary>
    /// SQLite transaction wrapper to allow for decorators
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Transport.RelationalDatabase.ITransactionWrapper" />
    public interface ISQLiteTransactionWrapper: ITransactionWrapper
    {

    }
}
