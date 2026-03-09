using System;
using System.Threading;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Transport.Memory.Basic
{
    [TestClass]
    public class ClearExpiredMessagesTests
    {
        [TestMethod]
        public void ClearMessages_Test()
        {
            var clear = new ClearExpiredMessages();
            Assert.ThrowsExactly<NotImplementedException>(() => clear.ClearMessages(CancellationToken.None));
        }
    }
}