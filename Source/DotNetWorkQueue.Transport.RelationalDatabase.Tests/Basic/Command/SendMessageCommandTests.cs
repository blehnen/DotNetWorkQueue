using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Command
{
    [TestClass]
    public class SendMessageCommandTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var id = Substitute.For<IMessage>();
            var message = new AdditionalMessageData();
            var test = new SendMessageCommand(id, message);
            Assert.AreEqual(id, test.MessageToSend);
            Assert.AreEqual(message, test.MessageData);
        }
    }
}
