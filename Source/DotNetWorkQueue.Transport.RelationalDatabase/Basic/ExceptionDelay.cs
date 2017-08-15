using System;
using System.Collections.Generic;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    public static class ExceptionDelay
    {
        /// <summary>
        /// Gets the default fatal exception delay time spans
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<TimeSpan> GetExceptionDelay()
        {
            var rc = new List<TimeSpan>(10)
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(8),
                TimeSpan.FromSeconds(13),
                TimeSpan.FromSeconds(21),
                TimeSpan.FromSeconds(34),
                TimeSpan.FromSeconds(55)
            };
            return rc;
        }
    }
}
