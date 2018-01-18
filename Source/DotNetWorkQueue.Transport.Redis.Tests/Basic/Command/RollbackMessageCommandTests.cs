using System;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Command;

using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Command
{
    public class RollbackMessageCommandTests
    {
        [Theory, AutoData]
        public void Create_Null_Constructor_Time_Ok(string number)
        {
            var test = new RollbackMessageCommand(new RedisQueueId(number), null);
            Assert.NotNull(test);
        }
        [Theory, AutoData]
        public void Create_Default(string number)
        {
            var id = new RedisQueueId(number);
            var test = new RollbackMessageCommand(id, null);
            Assert.Equal(id, test.Id);
            Assert.Null(test.IncreaseQueueDelay);

            TimeSpan? time = TimeSpan.MinValue;
            test = new RollbackMessageCommand(id, time);
            Assert.Equal(id, test.Id);
            Assert.Equal(time, test.IncreaseQueueDelay);
        }
    }
}
