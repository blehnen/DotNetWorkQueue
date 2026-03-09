using DotNetWorkQueue.Factory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Factory
{
    [TestClass]
    public class GetTimeFactoryNoOpTests
    {
        [TestMethod]
        public void Create()
        {
            var test = new GetTimeFactoryNoOp();
            Assert.IsNull(test.Create());
        }
    }
}
