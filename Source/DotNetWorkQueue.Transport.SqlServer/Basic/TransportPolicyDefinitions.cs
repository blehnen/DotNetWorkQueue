using Polly;

namespace DotNetWorkQueue.Transport.SqlServer.Basic
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
        public static string RetryCommandHandler => "SqlServerRetryCommandHandler";

        /// <summary>
        /// A policy for retrying a failed command
        /// </summary>
        /// <value>
        /// A policy for retrying a failed command
        /// </value>
        /// <remarks><seealso cref="Policy"></seealso> is the expected type</remarks>
        public static string RetryCommandHandlerAsync => "SqlServerRetryCommandHandlerAsync";

        /// <summary>
        /// A policy for retrying a failed query
        /// </summary>
        /// <value>
        /// The retry query handler.
        /// </value>
        public static string RetryQueryHandler => "SqlServerRetryQueryHandler";
    }
}
