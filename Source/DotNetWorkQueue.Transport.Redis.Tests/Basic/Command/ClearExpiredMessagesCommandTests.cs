using DotNetWorkQueue.Transport.Redis.Basic.Command;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Command
{
    public class ClearExpiredMessagesCommandTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = new ClearExpiredMessagesCommand();
            Assert.NotNull(test);
        }
    }
}
