using System;
using System.Threading;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Tests.Transport.Memory.Basic
{
    public class ClearExpiredMessagesTests
    {
        [Fact()]
        public void ClearMessages_Test()
        {
            var clear = new ClearExpiredMessages();
            Assert.Throws<NotImplementedException>(() => clear.ClearMessages(CancellationToken.None));
        }
    }
}