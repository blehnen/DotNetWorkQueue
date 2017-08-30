using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests
{
    public class IntegrationConnectionInfo: IDisposable //noop for now
    {
        public string ConnectionString => "none";

        public void Dispose()
        {

        }
    }
}
