using DotNetWorkQueue.Transport.PostgreSQL.Basic;

namespace DotNetWorkQueue.Transport.PostgreSQL
{
    /// <summary>
    /// Returns the options class
    /// </summary>
    public interface IPostgreSqlMessageQueueTransportOptionsFactory
    {
        /// <summary>
        /// Returns the options class
        /// </summary>
        /// <returns></returns>
        PostgreSqlMessageQueueTransportOptions Create();
    }
}
