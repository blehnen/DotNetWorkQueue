using System;
using DotNetWorkQueue.Logging;
using Xunit;

namespace DotNetWorkQueue.Tests.Logging
{
    public class NullLoggerProviderTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = Create();
            var log = test.GetLogger("Test");
            Assert.True(log.Invoke(LogLevel.Debug, () => string.Empty));
            Assert.True(log.Invoke(LogLevel.Debug, () => string.Empty, new Exception("test")));
            Assert.True(log.Invoke(LogLevel.Debug, () => string.Empty, new Exception("test"), string.Empty));
        }
        [Fact]
        public void Create_OpenNestedContext()
        {
            var test = Create();
            using (test.OpenNestedContext("test"))
            {

            }

        }
        [Fact]
        public void Create_OpenMappedContext()
        {
            var test = Create();
            using (test.OpenMappedContext("test", "test"))
            {

            }
        }
        private NullLoggerProvider Create()
        {
            return new NullLoggerProvider();
        }
    }
}
