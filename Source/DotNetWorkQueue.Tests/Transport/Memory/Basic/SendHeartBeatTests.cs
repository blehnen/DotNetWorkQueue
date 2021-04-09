using System;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Tests.Transport.Memory.Basic
{
    public class SendHeartBeatTests
    {
        [Fact()]
        public void Send_Test()
        {
            var send = new SendHeartBeat();
            Assert.Throws<NotImplementedException>(() => send.Send(null));
        }
    }
}