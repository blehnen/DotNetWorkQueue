using DotNetWorkQueue.Transport.Redis.Basic;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    public class RedisMessageTests
    {
        [Fact]
        public void Create_Null_Message_OK()
        {
            var test = new RedisMessage(null, null, false);
            Assert.Null(test.Message);
        }
        [Fact]
        public void Create_Message()
        {
            var message = Substitute.For<IReceivedMessageInternal>();
            var test = new RedisMessage("1", message, false);
            Assert.Equal(message, test.Message);
            Assert.Equal("1", test.MessageId);
        }
        [Fact]
        public void Create_Null_Message_Expired_False()
        {
            var test = new RedisMessage("1", null, false);
            Assert.False(test.Expired);
        }
        [Fact]
        public void Create_Null_Message_Expired_True()
        {
            var test = new RedisMessage("1", null, true);
            Assert.True(test.Expired);
        }
    }
}
