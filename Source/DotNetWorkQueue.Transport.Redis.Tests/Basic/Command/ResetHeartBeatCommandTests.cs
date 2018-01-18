using DotNetWorkQueue.Transport.Redis.Basic.Command;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Command
{
    public class ResetHeartBeatCommandTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = new ResetHeartBeatCommand();
            Assert.NotNull(test);
        }
    }
}
