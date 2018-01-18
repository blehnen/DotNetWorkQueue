using System;
using DotNetWorkQueue.Tests.IoC;
using Xunit;

namespace DotNetWorkQueue.Tests
{
    [Collection("IoC")]
    public class CreateQueueStatusTests
    {
        [Fact]
        public void Create_Null_Services_Fails()
        {
            using (var test = new QueueStatusContainer(null))
            {
                Assert.Throws<NullReferenceException>(
                    delegate
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        test.CreateStatus();
                    });
            }
        }
        [Fact]
        public void Create_Status()
        {
            using (var test = new QueueStatusContainer())
            {
                using (test.CreateStatus())
                {

                }
            }
        }

        [Fact]
        public void Create_Status_Provider()
        {
            using (var test = new QueueStatusContainer())
            {
                Assert.NotNull(test.CreateStatusProvider<CreateContainerTest.NoOpDuplexTransport>("queue", "connection"));
                Assert.NotNull(test.CreateStatusProvider<CreateContainerTest.NoOpDuplexTransport>(container => { },
                    "queue", "connection"));
            }
        }
    }
}
