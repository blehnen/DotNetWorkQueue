using System.Collections.Generic;
namespace DotNetWorkQueue.Transport.Memory.Basic
{
    internal class GetPreviousMessageErrorsNoOp: IGetPreviousMessageErrors
    {
        public IReadOnlyDictionary<string, int> Get(IMessageId id)
        {
            return new Dictionary<string, int>();
        }
    }
}
