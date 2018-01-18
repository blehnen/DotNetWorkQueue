namespace DotNetWorkQueue.Transport.PostgreSQL.Decorator
{
    /// <summary>
    /// Shared constants for the retry decorators
    /// </summary>
    internal class RetryConstants
    {
        /// <summary>
        /// The retry count
        /// </summary>
        public const int RetryCount = 3;
        /// <summary>
        /// The minimum wait in Milliseconds
        /// </summary>
        public const int MinWait = 100;
        /// <summary>
        /// The maximum wait in Milliseconds
        /// </summary>
        public const int MaxWait = 1000;
    }
}
