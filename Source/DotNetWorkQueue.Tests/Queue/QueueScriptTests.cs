using DotNetWorkQueue.Queue;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class QueueScriptTests
    {
        [TestMethod]
        public void HasScript()
        {
            var script = new QueueScript("true", true);
            Assert.IsTrue(script.HasScript);
        }
    }
}
