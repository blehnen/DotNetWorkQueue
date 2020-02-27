using System.Threading;
namespace DotNetWorkQueue.Tests.Queue
{
    public class ClearErrorMessagesNoOp: IClearErrorMessages
    {
        public long ClearMessages(CancellationToken cancelToken)
        {
            return 0;
        }
    }
}
