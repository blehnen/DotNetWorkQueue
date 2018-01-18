using System;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Command
{
    public class ResetHeartBeatCommandTests
    {
        [Fact]
        public void Create_Default()
        {
            const int id = 293;
            var date = DateTime.Now;
            var message = new MessageToReset(id, date);
            var test = new ResetHeartBeatCommand(message);
            Assert.Equal(message, test.MessageReset);
        }
    }
}
