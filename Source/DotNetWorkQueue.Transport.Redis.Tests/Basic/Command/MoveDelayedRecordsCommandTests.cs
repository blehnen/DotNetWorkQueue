using System.Threading;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Command
{
    public class MoveDelayedRecordsCommandTests
    {
        [Fact]
        public void Create_Default()
        {
            using (var cancel = new CancellationTokenSource())
            {
                var test = new MoveDelayedRecordsCommand(cancel.Token);
                Assert.NotNull(test);
            }
        }
    }
}
