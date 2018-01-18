using System;
using DotNetWorkQueue.Transport.Redis.Basic.Time;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Time
{
    public class SntpTimeConfigurationTests
    {
        [Fact]
        public void Create()
        {
            var test = new SntpTimeConfiguration();
            Assert.Equal(TimeSpan.FromSeconds(900), test.RefreshTime);
            Assert.Equal(123, test.Port);
            Assert.Equal("pool.ntp.org", test.Server);

            test.RefreshTime = TimeSpan.FromSeconds(100);
            Assert.Equal(TimeSpan.FromSeconds(100), test.RefreshTime);

            test.Port = 567;
            Assert.Equal(567, test.Port);

            test.Server = "test";
            Assert.Equal("test", test.Server);
        }
    }
}
