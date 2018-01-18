using DotNetWorkQueue.Transport.SqlServer.Basic;

namespace DotNetWorkQueue.Transport.SqlServer
{
    /// <summary>
    /// Returns the options class
    /// </summary>
    public interface ISqlServerMessageQueueTransportOptionsFactory
    {
        /// <summary>
        /// Returns the options class
        /// </summary>
        /// <returns></returns>
        SqlServerMessageQueueTransportOptions Create();
    }
}
