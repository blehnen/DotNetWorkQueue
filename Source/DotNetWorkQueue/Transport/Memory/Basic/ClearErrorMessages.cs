using System.Threading;

namespace DotNetWorkQueue.Transport.Memory.Basic
{
    /// <summary>
    /// A NoOp implementation of <seealso cref="DotNetWorkQueue.IClearErrorMessages" /> for the memory transport
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IClearErrorMessages" />
    public class ClearErrorMessages: IClearErrorMessages
    {
        /// <inheritdoc />
        public long ClearMessages(CancellationToken cancelToken)
        {
            return 0; //memory queue only keeps track of the number of errors; there is nothing to remove
        }
    }
}
