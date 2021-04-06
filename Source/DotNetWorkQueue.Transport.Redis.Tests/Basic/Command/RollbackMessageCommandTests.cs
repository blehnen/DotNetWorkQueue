using System;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Command
{
    public class RollbackMessageCommandTests
    {
        [Theory, AutoData]
        public void Create_Null_Constructor_Time_Ok(string number)
        {
            var test = new RollbackMessageCommand<string>(null, number, null);
            Assert.NotNull(test);
        }
        [Theory, AutoData]
        public void Create_Default(string number)
        {
            var test = new RollbackMessageCommand<string>(null, number, null);
            Assert.Equal(number, test.QueueId);
            Assert.Null(test.IncreaseQueueDelay);

            TimeSpan? time = TimeSpan.MinValue;
            test = new RollbackMessageCommand<string>(null, number, time);
            Assert.Equal(number, test.QueueId);
            Assert.Equal(time, test.IncreaseQueueDelay);
        }
    }
}
