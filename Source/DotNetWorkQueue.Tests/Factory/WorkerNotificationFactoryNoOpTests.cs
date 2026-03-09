using DotNetWorkQueue.Factory;
using DotNetWorkQueue.Queue;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Factory
{
    [TestClass]
    public class WorkerNotificationFactoryNoOpTests
    {
        [TestMethod]
        public void Create_Test()
        {
            var factory = new WorkerNotificationFactoryNoOp();
            var worker = factory.Create();
            Assert.IsNotNull(worker);
            Assert.IsExactInstanceOfType<WorkerNotificationNoOp>(worker);
        }
    }
}