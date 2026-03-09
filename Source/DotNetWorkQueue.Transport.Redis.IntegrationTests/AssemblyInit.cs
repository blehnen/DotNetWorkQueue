using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotNetWorkQueue.IntegrationTests.Shared;

namespace DotNetWorkQueue.Transport.Redis.Integration.Tests
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
