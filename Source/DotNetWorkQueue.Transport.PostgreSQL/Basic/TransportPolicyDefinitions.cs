using Polly;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <summary>
    /// 
    /// </summary>
    public static class TransportPolicyDefinitions
    {
        /// <summary>
        /// A policy for retrying a failed command
        /// </summary>
        /// <value>
        /// A policy for retrying a failed command
        /// </value>
        /// <remarks><seealso cref="Policy"></seealso> is the expected type</remarks>
        public static string RetryCommandHandler => "PostgreSQLRetryCommandHandler";

        /// <summary>
        /// A policy for retrying a failed command
        /// </summary>
        /// <value>
        /// A policy for retrying a failed command
        /// </value>
        /// <remarks><seealso cref="Policy"></seealso> is the expected type</remarks>
        public static string RetryCommandHandlerAsync => "PostgreSQLRetryCommandHandlerAsync";

        /// <summary>
        /// A policy for retrying a failed query
        /// </summary>
        /// <value>
        /// The retry query handler.
        /// </value>
        public static string RetryQueryHandler => "PostgreSQLRetryQueryHandler";
    }
}
