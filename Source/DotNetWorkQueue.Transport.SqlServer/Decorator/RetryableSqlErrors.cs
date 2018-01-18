namespace DotNetWorkQueue.Transport.SqlServer.Decorator
{
    /// <summary>
    /// Indicates which SQL errors the retry decorator will retry
    /// </summary>
    internal enum RetryableSqlErrors
    {
        SqlTimeout = -2,
        SqlOutOfMemory = 701,
        SqlOutOfLocks = 1204,
        SqlDeadlockVictim = 1205,
        SqlLockRequestTimeout = 1222,
        SqlTimeoutWaitingForMemoryResource = 8645,
        SqlLowMemoryCondition = 8651,
        SqlWordbreakerTimeout = 30053
    }
}
