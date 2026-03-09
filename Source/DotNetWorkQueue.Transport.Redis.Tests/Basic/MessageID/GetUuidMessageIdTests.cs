using System;
using DotNetWorkQueue.Transport.Redis.Basic.MessageID;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.MessageID
{
    [TestClass]
    public class GetUuidMessageIdTests
    {
        [TestMethod]
        public void Create()
        {
            var test = new GetUuidMessageId();
            Assert.IsInstanceOfType<Guid>(new Guid(test.Create().Id.Value.ToString()));
        }
    }
}
