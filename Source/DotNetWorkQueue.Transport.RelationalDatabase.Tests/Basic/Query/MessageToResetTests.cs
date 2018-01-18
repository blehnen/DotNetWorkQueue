using System;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    public class MessageToResetTests
    {
        [Fact]
        public void Create_Default()
        {
            var date = DateTime.Now;
            var test = new MessageToReset(100, date);
            Assert.Equal(100, test.QueueId);
            Assert.Equal(date, test.HeartBeat);
        }
    }
}
