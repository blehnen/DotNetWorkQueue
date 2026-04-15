using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotNetWorkQueue.IntegrationTests.Shared;

[assembly: DoNotParallelize]

namespace DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests
{
    [TestClass]
    public static class AssemblyInit
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            MsTestHelper.ClearSynchronizationContext();
        }
    }
}
