using System;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Messages;

using Xunit;

namespace DotNetWorkQueue.Tests.Messages
{
    public class RpcTimeoutTests
    {
        [Theory, AutoData]
        public void Get_Timeout(TimeSpan value)
        {
            var test = new RpcTimeout(value);
            Assert.Equal(test.Timeout, value);
        }
    }
}
