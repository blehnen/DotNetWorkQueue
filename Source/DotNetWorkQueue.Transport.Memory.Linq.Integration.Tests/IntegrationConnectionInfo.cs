using System;

namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests
{
    public class IntegrationConnectionInfo: IDisposable //noop for now
    {
        public string ConnectionString => "none";

        public void Dispose()
        {

        }
    }
}
