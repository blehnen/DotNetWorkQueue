using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotNetWorkQueue.Dashboard.Api.Integration.Tests.Helpers;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests
{
    [TestClass]
    [Retry(1)]
    public static class AssemblyInit
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            SynchronizationContext.SetSynchronizationContext(null);
        }

        /// <summary>
        /// Returns any service containers this run started. Nothing is started here: this
        /// assembly covers several transports, so each endpoint starts on first use and a run
        /// filtered to the in-process transports starts none at all.
        /// </summary>
        [AssemblyCleanup]
        public static void Cleanup()
        {
            ConnectionStrings.Shutdown();
        }
    }
}
