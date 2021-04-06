using System;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    public class MessageToResetTests
    {
        [Fact]
        public void Create_Default()
        {
            var date = DateTime.UtcNow;
            var test = new MessageToReset<long>(100, date, null);
            Assert.Equal(100, test.QueueId);
            Assert.Equal(date, test.HeartBeat);
        }
    }
}
