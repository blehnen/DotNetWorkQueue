using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.Shared.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic
{
    public class MessageQueueIdTests
    {
        [Fact]
        public void Create_Default()
        {
            long id = 1;
            var test = new MessageQueueId<long>(id);
            Assert.Equal(id, test.Id.Value);
            Assert.True(test.HasValue);
        }
        [Fact]
        public void Create_Default_ToString()
        {
            long id = 1;
            var test = new MessageQueueId<long>(id);
            Assert.Equal("1", test.ToString());
        }
        [Fact]
        public void Create_Default_0()
        {
            long id = 0;
            var test = new MessageQueueId<long>(id);
            Assert.Equal(id, test.Id.Value);
            Assert.False(test.HasValue);
        }
    }
}
