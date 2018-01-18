using System;
using Xunit;

namespace DotNetWorkQueue.Tests
{
    [Collection("IoC")]
    public class SchedulerCreatorTests
    {
        [Fact]
        public void Create_Null_Services_Fails()
        {
            using (var test = new SchedulerContainer(null))
            {
                Assert.Throws<NullReferenceException>(
                    delegate
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        test.CreateTaskFactory();
                    });
            }
        }
        [Fact]
        public void Create_TaskScheduler()
        {
            using (var test = new SchedulerContainer())
            {
                using (test.CreateTaskScheduler())
                {

                }
            }
        }
        [Fact]
        public void Create_CreateTaskFactory()
        {
            using (var test = new SchedulerContainer())
            {
                Assert.NotNull(test.CreateTaskFactory());
            }
        }
        [Fact]
        public void Create_CreateTaskFactory2()
        {
            using (var test = new SchedulerContainer())
            {
                Assert.NotNull(test.CreateTaskFactory(test.CreateTaskScheduler()));
            }
        }
    }
}
