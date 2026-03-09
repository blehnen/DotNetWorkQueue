using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests
{
    [TestClass]
    public class SchedulerCreatorTests
    {
        [TestMethod]
        public void Create_Null_Services_Fails()
        {
            using (var test = new SchedulerContainer(null))
            {
                Assert.ThrowsExactly<NullReferenceException>(
                    delegate
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        test.CreateTaskFactory();
                    });
            }
        }
        [TestMethod]
        public void Create_TaskScheduler()
        {
            using (var test = new SchedulerContainer())
            {
                using (test.CreateTaskScheduler())
                {

                }
            }
        }
        [TestMethod]
        public void Create_CreateTaskFactory()
        {
            using (var test = new SchedulerContainer())
            {
                Assert.IsNotNull(test.CreateTaskFactory());
            }
        }
        [TestMethod]
        public void Create_CreateTaskFactory2()
        {
            using (var test = new SchedulerContainer())
            {
                Assert.IsNotNull(test.CreateTaskFactory(test.CreateTaskScheduler()));
            }
        }
    }
}
