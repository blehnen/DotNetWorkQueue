using System.Threading;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    [TestClass]
    public class FindMessagesToResetByHeartBeatQueryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            using (var cancel = new CancellationTokenSource())
            {
                var test = new FindMessagesToResetByHeartBeatQuery<long>(cancel.Token);
                Assert.AreEqual(cancel.Token, test.Cancellation);
            }
        }
    }
}
