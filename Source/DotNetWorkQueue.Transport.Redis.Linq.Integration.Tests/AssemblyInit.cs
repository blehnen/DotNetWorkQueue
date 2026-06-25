using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotNetWorkQueue.IntegrationTests.Shared;

namespace DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests
{
    [TestClass]
    public static class AssemblyInit
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            MsTestHelper.ClearSynchronizationContext();

            // Raise the worker-thread floor so genuinely-synchronous Redis EVAL integration tests do not
            // false-fail under burst injection on SE.Redis 3.x (host thread-pool-sizing responsibility, not a
            // library bug — see ROADMAP #161). Redis transport only: each Jenkins stage is its own process,
            // and only the Redis sync-EVAL path needs this headroom. IOCP min is passed back unchanged; the
            // SetMinThreads return value is intentionally ignored (matches StarvationBaselineTests' pattern).
            ThreadPool.GetMinThreads(out var currentMinWorker, out var currentMinIocp);
            var targetMinWorker = Math.Max(Environment.ProcessorCount * 4, 200);
            if (currentMinWorker < targetMinWorker)
                ThreadPool.SetMinThreads(targetMinWorker, currentMinIocp);
        }
    }
}
