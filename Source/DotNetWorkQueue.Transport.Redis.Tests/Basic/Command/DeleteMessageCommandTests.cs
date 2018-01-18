using AutoFixture.Xunit2;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Command
{
    public class DeleteMessageCommandTests
    {
        [Theory, AutoData]
        public void Create_Default(string number)
        {
            var id = new RedisQueueId(number);
            var test = new DeleteMessageCommand(id);
            Assert.Equal(id, test.Id);
        }
    }
}
