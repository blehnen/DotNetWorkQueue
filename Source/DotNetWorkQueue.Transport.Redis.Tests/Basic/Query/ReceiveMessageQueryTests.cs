using DotNetWorkQueue.Transport.Redis.Basic.Query;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Query
{
    [TestClass]
    public class ReceiveMessageQueryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var context = Substitute.For<IMessageContext>();
            var test = new ReceiveMessageQuery(context);
            Assert.AreEqual(context, test.MessageContext);
        }
    }
}
