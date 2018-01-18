using System;
using DotNetWorkQueue.Transport.Redis.Basic.MessageID;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.MessageID
{
    public class GetUuidMessageIdTests
    {
        [Fact]
        public void Create()
        {
            var test = new GetUuidMessageId();
            Assert.IsAssignableFrom<Guid>(new Guid(test.Create().Id.Value.ToString()));
        }
    }
}
