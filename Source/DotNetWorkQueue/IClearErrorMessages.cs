using System.Threading;
namespace DotNetWorkQueue
{
    public interface IClearErrorMessages
    {
        /// <summary>
        /// Clears messages in an error status from the queue
        /// </summary>
        /// <param name="cancelToken">The cancel token. When signaled, processing should stop as soon as possible</param>
        long ClearMessages(CancellationToken cancelToken);
    }
}
