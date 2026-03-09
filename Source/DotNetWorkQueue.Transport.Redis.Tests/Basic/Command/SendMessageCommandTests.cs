using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Command
{
    [TestClass]
    public class SendMessageCommandTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var message = Substitute.For<IMessage>();
            var data = Substitute.For<IAdditionalMessageData>();
            var test = new SendMessageCommand(message, data);
            Assert.AreEqual(message, test.MessageToSend);
            Assert.AreEqual(data, test.MessageData);
        }
    }
}
