using DotNetWorkQueue.Transport.SQLite.Shared.Basic;

namespace DotNetWorkQueue.Transport.SQLite.Shared
{
    /// <summary>
    /// Returns the options class
    /// </summary>
    public interface ISqLiteMessageQueueTransportOptionsFactory
    {
        /// <summary>
        /// Returns the options class
        /// </summary>
        /// <returns></returns>
        SqLiteMessageQueueTransportOptions Create();
    }
}
