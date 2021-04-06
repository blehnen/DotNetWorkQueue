using System.Threading;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    public class FindExpiredMessagesToDeleteQueryTests
    {
        [Fact]
        public void Create_Default()
        {
            using (var cancel = new CancellationTokenSource())
            {
                var test = new FindExpiredMessagesToDeleteQuery<long>(cancel.Token);
                Assert.Equal(cancel.Token, test.Cancellation);
            }
        }
    }
}
