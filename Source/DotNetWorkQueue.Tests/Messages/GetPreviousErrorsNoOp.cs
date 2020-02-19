using System.Collections.Generic;
namespace DotNetWorkQueue.Tests.Messages
{
    internal class GetPreviousErrorsNoOp: IGetPreviousMessageErrors
    {
        public IReadOnlyDictionary<string, int> Get(IMessageId id)
        {
            return new Dictionary<string, int>();
        }
    }
}
