using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    public static class DefaultQueueDelay
    {
        public static IEnumerable<TimeSpan> GetDefaultQueueDelay()
        {
            var rc = new List<TimeSpan>(21)
            {
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(2000)
            };
            return rc;
        }
    }
}
