using System.Collections.Generic;
using DotNetWorkQueue;
namespace SampleShared
{
    public static class HandleResults
    {
        public static void Handle(IEnumerable<IQueueOutputMessage> results, Serilog.ILogger log)
        {
            foreach (var result in results)
            {
                if (result.HasError)
                {
                    log.Error(result.SendingException?.ToString());
                }
            }
        }
    }
}
