using System.Data;
namespace DotNetWorkQueue.Transport.RelationalDatabase
{
    public interface IDbConnectionFactory
    {
        /// <summary>
        /// Creates a new connection to the database
        /// </summary>
        /// <returns></returns>
        IDbConnection Create();
    }
}
