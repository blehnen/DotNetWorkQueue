using System.Threading;

namespace DotNetWorkQueue.IntegrationTests.Shared
{
    /// <summary>
    /// Clears the SynchronizationContext so async tests behave the same as under xUnit.
    /// xUnit does not install a SynchronizationContext; MSTest does, which can cause
    /// deadlocks when library code is missing ConfigureAwait(false) in third-party dependencies.
    /// </summary>
    public static class MsTestHelper
    {
        public static void ClearSynchronizationContext()
        {
            SynchronizationContext.SetSynchronizationContext(null);
        }
    }
}
