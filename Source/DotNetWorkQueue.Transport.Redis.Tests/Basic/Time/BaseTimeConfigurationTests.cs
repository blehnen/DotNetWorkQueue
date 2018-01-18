using System;
using DotNetWorkQueue.Configuration;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Time
{
    public class BaseTimeConfigurationTests
    {
        [Fact]
        public void Create()
        {
            var test = new BaseTimeConfiguration();
            Assert.Equal(TimeSpan.FromSeconds(900), test.RefreshTime);
            test.RefreshTime = TimeSpan.FromSeconds(100);
            Assert.Equal(TimeSpan.FromSeconds(100), test.RefreshTime);
        }
    }
}
