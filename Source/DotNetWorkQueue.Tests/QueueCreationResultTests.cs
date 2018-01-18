using AutoFixture.Xunit2;
using Xunit;

namespace DotNetWorkQueue.Tests
{
    public class QueueCreationResultTests
    {
        [Fact]
        public void Create_Ok()
        {
            var test = new QueueCreationResult(QueueCreationStatus.None, null);
            Assert.Equal(QueueCreationStatus.None, test.Status);
        }
        [Fact]
        public void GetSet_Status()
        {
            var test = new QueueCreationResult(QueueCreationStatus.AlreadyExists, null);
            Assert.Equal(QueueCreationStatus.AlreadyExists, test.Status);
        }
        [Theory, AutoData]
        public void GetSet_ErrorMessage(string message)
        {
            var test = new QueueCreationResult(QueueCreationStatus.AlreadyExists, message);
            Assert.Equal(message, test.ErrorMessage);
        }
        [Theory, AutoData]
        public void GetSet_ErrorMessage_True(string message)
        {
            var test = new QueueCreationResult(QueueCreationStatus.Success, message);
            Assert.True(test.Success);
        }
        [Theory, AutoData]
        public void GetSet_ErrorMessage_False(string message)
        {
            var test = new QueueCreationResult(QueueCreationStatus.ConfigurationError, message);
            Assert.False(test.Success);
        }
    }
}
