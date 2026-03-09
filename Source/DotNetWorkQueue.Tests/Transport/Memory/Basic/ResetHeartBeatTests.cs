using System.Threading;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Transport.Memory.Basic
{
    [TestClass]
    public class ResetHeartBeatTests
    {
        [TestMethod]
        public void Reset_Test()
        {
            var heart = new ResetHeartBeat();
            Assert.IsEmpty(heart.Reset(CancellationToken.None));
        }
    }
}