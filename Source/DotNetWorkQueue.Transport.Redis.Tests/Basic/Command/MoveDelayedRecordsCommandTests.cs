using System.Threading;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Command
{
    [TestClass]
    public class MoveDelayedRecordsCommandTests
    {
        [TestMethod]
        public void Create_Default()
        {
            using (var cancel = new CancellationTokenSource())
            {
                var test = new MoveDelayedRecordsCommand(cancel.Token);
                Assert.IsNotNull(test);
            }
        }
    }
}
