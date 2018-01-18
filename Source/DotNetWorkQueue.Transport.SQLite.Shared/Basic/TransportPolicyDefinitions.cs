using Polly;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic
{
    /// <summary>
    /// 
    /// </summary>
    public static class TransportPolicyDefinitions
    {
        /// <summary>
        /// A policy for retrying when a new transaction fails to start
        /// </summary>
        /// <value>
        /// A policy for retrying when a new transaction fails to start
        /// </value>
        /// <remarks><seealso cref="Policy"></seealso> is the expected type</remarks>
        public static string BeginTransaction => "SqliteBeginTransaction";
    }
}
