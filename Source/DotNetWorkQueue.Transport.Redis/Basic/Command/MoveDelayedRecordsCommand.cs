using System.Threading;

namespace DotNetWorkQueue.Transport.Redis.Basic.Command
{
    /// <summary>
    /// Move delayed records to the pending queue command
    /// </summary>
    public class MoveDelayedRecordsCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MoveDelayedRecordsCommand"/> class.
        /// </summary>
        /// <param name="token">The token.</param>
        public MoveDelayedRecordsCommand(CancellationToken token)
        {
            Token = token;
        }
        /// <summary>
        /// Gets the token.
        /// </summary>
        /// <value>
        /// The token.
        /// </value>
        public CancellationToken Token { get; }
    }
}
