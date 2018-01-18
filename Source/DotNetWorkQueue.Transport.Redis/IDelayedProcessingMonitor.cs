namespace DotNetWorkQueue.Transport.Redis
{
    /// <summary>
    /// Moves delayed records to the pending queue
    /// </summary>
    public interface IDelayedProcessingMonitor : IMonitor
    {

    }
}
