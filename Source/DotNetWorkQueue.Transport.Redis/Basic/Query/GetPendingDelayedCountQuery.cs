using DotNetWorkQueue.Transport.Shared;

namespace DotNetWorkQueue.Transport.Redis.Basic.Query
{
    /// <inheritdoc />
    /// <summary>
    /// Returns the current delayed pending count
    /// </summary>
    public class GetPendingDelayedCountQuery : IQuery<long>
    {
    }
}
