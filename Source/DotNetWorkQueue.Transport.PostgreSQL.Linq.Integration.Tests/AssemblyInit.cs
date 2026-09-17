using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests
{
    [TestClass]
    [Retry(1)]
    public static class AssemblyInit
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            MsTestHelper.ClearSynchronizationContext();

            // Resolve the endpoint here so a container start, if this run needs one, is paid
            // once during initialization instead of inside the first test to touch the database.
            ConnectionInfo.EnsureStarted();
        }

        [AssemblyCleanup]
        public static void Cleanup()
        {
            ConnectionInfo.Shutdown();
        }
    }
}
