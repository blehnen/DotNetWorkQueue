using DotNetWorkQueue.Queue;
using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class QueueScriptTests
    {
        [Fact]
        public void HasScript()
        {
            var script = new QueueScript("true", true);
            Assert.True(script.HasScript);
        }
    }
}
