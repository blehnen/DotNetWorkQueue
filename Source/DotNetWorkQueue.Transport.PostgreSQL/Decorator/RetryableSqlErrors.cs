using System.Collections.Generic;

namespace DotNetWorkQueue.Transport.PostgreSQL.Decorator
{
    internal static class RetryablePostGreErrors
    {
        public static IEnumerable<string> Errors
        {
            get { yield return "40P01"; }
        }
    }
}
