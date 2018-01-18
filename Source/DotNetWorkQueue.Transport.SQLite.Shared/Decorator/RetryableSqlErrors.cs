namespace DotNetWorkQueue.Transport.SQLite.Shared.Decorator
{
    /// <summary>
    /// Indicates which SQL errors the retry decorator will retry
    /// </summary>
    public enum RetryableSqlErrors
    {
        /// <summary>
        /// The database is busy
        /// </summary>
        DatabaseIsBusy = 5,
        /// <summary>
        /// The database is locked
        /// </summary>
        DatabaseIsLocked = 6
    }
}
