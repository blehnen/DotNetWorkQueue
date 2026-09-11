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

        [TestMethod]
        public void SendAsync_Test()
        {
            //the memory transport has no heartbeat, so reaching either member is a bug in the caller
            var send = new SendHeartBeat();
            Assert.ThrowsExactly<NotImplementedException>(() => send.SendAsync(null));
        }
    }
}