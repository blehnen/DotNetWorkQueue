using System;
using DotNetWorkQueue.Logging;
using Xunit;
namespace DotNetWorkQueue.Tests.Logging
{
    public class NullLoggerTests
    {
        [Fact()]
        public void Log_Test()
        {
            var logger = new NullLogger();
            logger.Log(() => new LogEntry(LoggingEventType.Debug, string.Empty));
        }

        [Fact()]
        public void Log_Test1()
        {
            var logger = new NullLogger();
            logger.Log(() => new LogEntry(LoggingEventType.Debug, string.Empty, new Exception()));
        }
    }
}