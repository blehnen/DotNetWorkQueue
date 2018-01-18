using DotNetWorkQueue.Transport.Redis.Basic.Command;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Command
{
    public class SendMessageCommandTests
    {
        [Fact]
        public void Create_Default()
        {
            var message = Substitute.For<IMessage>();
            var data = Substitute.For<IAdditionalMessageData>();
            var test = new SendMessageCommand(message, data);
            Assert.Equal(message, test.MessageToSend);
            Assert.Equal(data, test.MessageData);
        }
    }
}
