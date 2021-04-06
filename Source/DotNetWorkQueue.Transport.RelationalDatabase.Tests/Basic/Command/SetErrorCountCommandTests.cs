using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Command
{
    public class SetErrorCountCommandTests
    {
        [Fact]
        public void Create_Default()
        {
            const int id = 19334;
            var type = "errorType";
            var test = new SetErrorCountCommand<long>(type, id);
            Assert.Equal(id, test.QueueId);
            Assert.Equal(type, test.ExceptionType);
        }
    }
}
