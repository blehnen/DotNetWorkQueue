using Xunit;

namespace DotNetWorkQueue.Tests
{
    public class QueueRemoveResultTests
    {
        [Fact]
        public void Create_SetStatus()
        {
            var test = new QueueRemoveResult(QueueRemoveStatus.DoesNotExist);
            Assert.Equal(QueueRemoveStatus.DoesNotExist, test.Status);
        }

        [Fact]
        public void Create_Success()
        {
            var test = new QueueRemoveResult(QueueRemoveStatus.DoesNotExist);
            Assert.False(test.Success);

            test = new QueueRemoveResult(QueueRemoveStatus.Success);
            Assert.True(test.Success);

            test = new QueueRemoveResult(QueueRemoveStatus.None);
            Assert.False(test.Success);
        }
    }
}
