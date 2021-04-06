using System;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Command
{
    public class ResetHeartBeatCommandTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = new ResetHeartBeatCommand<string>(new MessageToReset<string>(string.Empty, DateTime.Now, null));
            Assert.NotNull(test);
        }
    }
}
