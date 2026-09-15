using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests
{
    [TestClass]
    [Retry(2)]
    public static class AssemblyInit
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            SynchronizationContext.SetSynchronizationContext(null);
        }
    }
}
