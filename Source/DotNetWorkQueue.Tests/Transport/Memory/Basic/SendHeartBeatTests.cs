using System;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Transport.Memory.Basic
{
    [TestClass]
    public class SendHeartBeatTests
    {
        [TestMethod]
        public void Send_Test()
        {
            var send = new SendHeartBeat();
            Assert.ThrowsExactly<NotImplementedException>(() => send.Send(null));
        }
    }
}