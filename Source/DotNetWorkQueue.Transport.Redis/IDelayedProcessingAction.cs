using System.Threading;

namespace DotNetWorkQueue.Transport.Redis
{
    /// <summary>
    /// Runs the delayed processing action; generally speaking, this will check for and move delayed records into the pending key
    /// </summary>
    internal interface IDelayedProcessingAction
    {
        /// <summary>
        /// Runs the action
        /// </summary>
        /// <param name="token">The cancel token.</param>
        /// <returns></returns>
        long Run(CancellationToken token);
    }
}
