using DotNetWorkQueue.Factory;
using Xunit;

namespace DotNetWorkQueue.Tests.Factory
{
    public class GetTimeFactoryNoOpTests
    {
        [Fact]
        public void Create()
        {
            var test = new GetTimeFactoryNoOp();
            Assert.Null(test.Create());
        }
    }
}
