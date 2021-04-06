using AutoFixture.Xunit2;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Command
{
    public class SendHeartBeatCommandTests
    {
        [Theory, AutoData]
        public void Create_Default(string number)
        {
            var test = new SendHeartBeatCommand<string>(number);
            Assert.Equal(number, test.QueueId);
        }
    }
}
