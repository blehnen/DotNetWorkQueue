using System.Threading;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Tests.Transport.Memory.Basic
{
    public class ResetHeartBeatTests
    {
        [Fact()]
        public void Reset_Test()
        {
            var heart = new ResetHeartBeat();
            Assert.Empty(heart.Reset(CancellationToken.None));
        }
    }
}