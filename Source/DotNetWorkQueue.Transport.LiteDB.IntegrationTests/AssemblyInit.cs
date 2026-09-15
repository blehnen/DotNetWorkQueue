using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotNetWorkQueue.IntegrationTests.Shared;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests
{
    [TestClass]
    [Retry(1)]
    public static class AssemblyInit
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            MsTestHelper.ClearSynchronizationContext();
        }
    }
}
