using System.Collections.Specialized;
using Microsoft.Extensions.Logging;
using Serilog;

namespace SampleShared
{
    public static class Helpers
    {
        public static string ReadSetting(this NameValueCollection collection, string key)
        {
            return collection[key] ?? string.Empty;
        }

        public static ILoggerFactory CreateForSerilog()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddFilter(level => level >= LogLevel.Trace)
                        .AddSerilog();
                }
            );
            return loggerFactory;
        }
    }
}
