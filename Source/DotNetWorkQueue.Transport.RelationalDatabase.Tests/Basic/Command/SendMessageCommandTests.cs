using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Command
{
    public class SendMessageCommandTests
    {
        [Fact]
        public void Create_Default()
        {
            var id = Substitute.For<IMessage>();
            var message = new AdditionalMessageData();
            var test = new SendMessageCommand(id, message);
            Assert.Equal(id, test.MessageToSend);
            Assert.Equal(message, test.MessageData);
        }
    }
}
