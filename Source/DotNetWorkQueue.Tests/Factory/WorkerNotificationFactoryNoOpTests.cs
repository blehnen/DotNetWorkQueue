using DotNetWorkQueue.Factory;
using DotNetWorkQueue.Queue;
using Xunit;

namespace DotNetWorkQueue.Tests.Factory
{
    public class WorkerNotificationFactoryNoOpTests
    {
        [Fact()]
        public void Create_Test()
        {
            var factory = new WorkerNotificationFactoryNoOp();
            var worker = factory.Create();
            Assert.NotNull(worker);
            Assert.IsType<WorkerNotificationNoOp>(worker);
        }
    }
}