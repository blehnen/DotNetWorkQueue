using DotNetWorkQueue.Transport.Memory.Basic.Message;
using Xunit;

namespace DotNetWorkQueue.Tests.Transport.Memory.Basic.Message
{
    public class RollbackMessageTests
    {
        [Fact()]
        public void Rollback_Test()
        {
            var message = new RollbackMessage();
            //no-op
            message.Rollback(null);
        }
    }
}