using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.TaskScheduling;
using NSubstitute;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.TaskScheduling
{
    [TestClass]
    public class SmartThreadPoolTaskSchedulerTests
    {
        [TestMethod]
        public void IsDisposed_False_By_Default()
        {
            using (var test = Create())
            {
                Assert.IsFalse(test.IsDisposed);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [TestMethod]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.IsTrue(test.IsDisposed);
            }
        }

        [TestMethod]
        public void Configuration_Not_Null()
        {
            using (var test = Create())
            {
                Assert.IsNotNull(test.Configuration);
            }
        }

        [TestMethod]
        public void Calling_Start_With_Max_Zero_Exception()
        {
            using (var test = Create(0, TimeSpan.MaxValue, false))
            {
                Assert.ThrowsExactly<ArgumentException>(
                    delegate
                    {
                        test.Start();
                    });
            }
        }

        [TestMethod]
        public void Configuration_ReadOnly_After_Start()
        {
            using (var test = Create())
            {
                test.Start();
                test.Configuration.Received(1).SetReadOnly();
            }
        }

        [TestMethod]
        public void Calling_Start_Disposed_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.ThrowsExactly<ObjectDisposedException>(
                    delegate
                    {
                        test.Start();
                    });
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [TestMethod]
        public void RoomForNew_Task_Disposed_Returns_False()
        {
            using (var test = Create())
            {
                test.Start();
                test.Dispose();
                Assert.AreEqual(RoomForNewTaskResult.No, test.RoomForNewTask);
            }
        }


        [TestMethod]
        public void RoomForNew_Task_NotStarted_Exception()
        {
            using (var test = Create())
            {
                Assert.ThrowsExactly<DotNetWorkQueueException>(
                    delegate
                    {
                        Console.WriteLine(test.RoomForNewTask);
                    });
            }
        }

        [TestMethod]
        public void RoomForNew_True()
        {
            using (var test = Create(true))
            {
                test.Start();
                Assert.AreEqual(RoomForNewTaskResult.RoomForTask, test.RoomForNewTask);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [TestMethod]
        public void RoomForNewWorkGroup_Task_Disposed_Returns_False()
        {
            using (var test = Create())
            {
                test.Start();
                test.Dispose();
                Assert.AreEqual(RoomForNewTaskResult.No, test.RoomForNewWorkGroupTask(Substitute.For<IWorkGroup>()));
            }
        }

        [TestMethod]
        public void RoomForNewWorkGroup_True()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<string>();
            using (var test = Create())
            {
                test.Start();
                var group = test.AddWorkGroup(value, 1);
                Assert.AreEqual(RoomForNewTaskResult.RoomForTask, test.RoomForNewWorkGroupTask(group));
            }
        }

        [TestMethod]
        public void AddTask_Disposed_Exception()
        {
            using (var test = Create())
            {
                test.Start();
                test.Dispose();
                Assert.ThrowsExactly<ObjectDisposedException>(
                    delegate
                    {
                        test.AddTask(new Task(() => { }));
                    });
            }
        }

        [TestMethod]
        public void AddTask()
        {
            using (var test = Create())
            {
                test.Start();
                test.AddTask(new Task(() => { Thread.Sleep(100); }));
            }
        }

        [TestMethod]
        public void AddTask_WorkGroup()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<string>();
            using (var test = Create())
            {
                test.Start();
                var group = test.AddWorkGroup(value, 1);
                var state = new StateInformation(group);
                var t = new Task(state1 => { Thread.Sleep(100); }, state);
                test.AddTask(t);
            }
        }

        [TestMethod]
        public void MaximumCurrencyLevel_1()
        {
            using (var test = Create())
            {
                test.Start();
                Assert.AreEqual(1, test.MaximumConcurrencyLevel);
            }
        }

        [TestMethod]
        public void AddWorkGroup_Disposed_Exception()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<string>();
            using (var test = Create())
            {
                test.Start();
                test.Dispose();
                Assert.ThrowsExactly<ObjectDisposedException>(
                    delegate
                    {
                        test.AddWorkGroup(value, 1);
                    });
            }
        }

        [TestMethod]
        public void AddWorkGroup_Disposed_Exception2()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<string>();
            using (var test = Create())
            {
                test.Start();
                test.Dispose();
                Assert.ThrowsExactly<ObjectDisposedException>(
                    delegate
                    {
                        test.AddWorkGroup(value, 1);
                    });
            }
        }

        [TestMethod]
        public void AddWorkGroup()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<string>();
            using (var test = Create())
            {
                test.Start();
                test.AddWorkGroup(value, 1);
            }
        }


        private SmartThreadPoolTaskScheduler Create(int max, TimeSpan wait, bool readonlyConfig)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var config = fixture.Create<ITaskSchedulerConfiguration>();

            config.MaximumThreads.Returns(max);
            config.WaitForThreadPoolToFinish.Returns(wait);
            if (readonlyConfig)
                config.IsReadOnly.Returns(true);
            fixture.Inject(config);

            return fixture.Create<SmartThreadPoolTaskScheduler>();
        }

        private SmartThreadPoolTaskScheduler Create(bool readonlyConfig = false)
        {
            return Create(1, TimeSpan.FromSeconds(5), readonlyConfig);
        }
    }
}
