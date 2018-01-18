using System.Threading;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    public class FindMessagesToResetByHeartBeatQueryTests
    {
        [Fact]
        public void Create_Default()
        {
            using (var cancel = new CancellationTokenSource())
            {
                var test = new FindMessagesToResetByHeartBeatQuery(cancel.Token);
                Assert.Equal(cancel.Token, test.Cancellation);
            }
        }
    }
}
