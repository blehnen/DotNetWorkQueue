namespace DotNetWorkQueue.Transport.SQLite.Shared
{
    public interface IDbDataSource
    {
        string DataSource(string connectionString);
    }
}
