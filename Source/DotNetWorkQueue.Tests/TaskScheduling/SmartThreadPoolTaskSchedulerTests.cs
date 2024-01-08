﻿using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.TaskScheduling;
using NSubstitute;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.TaskScheduling
{
    public class SmartThreadPoolTaskSchedulerTests
    {
        [Fact]
        public void IsDisposed_False_By_Default()
        {
            using (var test = Create())
            {
                Assert.False(test.IsDisposed);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [Fact]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.True(test.IsDisposed);
            }
        }

        [Fact]
        public void Configuration_Not_Null()
        {
            using (var test = Create())
            {
                Assert.NotNull(test.Configuration);
            }
        }

        [Fact]
        public void Calling_Start_With_Max_Zero_Exception()
        {
            using (var test = Create(0, TimeSpan.MaxValue, false))
            {
                Assert.Throws<ArgumentException>(
                    delegate
                    {
                        test.Start();
                    });
            }
        }

        [Fact]
        public void Configuration_ReadOnly_After_Start()
        {
            using (var test = Create())
            {
                test.Start();
                test.Configuration.Received(1).SetReadOnly();
            }
        }

        [Fact]
        public void Calling_Start_Disposed_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
                    delegate
                    {
                        test.Start();
                    });
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [Fact]
        public void RoomForNew_Task_Disposed_Returns_False()
        {
            using (var test = Create())
            {
                test.Start();
                test.Dispose();
                Assert.Equal(RoomForNewTaskResult.No, test.RoomForNewTask);
            }
        }


        [Fact]
        public void RoomForNew_Task_NotStarted_Exception()
        {
            using (var test = Create())
            {
                Assert.Throws<DotNetWorkQueueException>(
                    delegate
                    {
                        Console.WriteLine(test.RoomForNewTask);
                    });
            }
        }

        [Fact]
        public void RoomForNew_True()
        {
            using (var test = Create(true))
            {
                test.Start();
                Assert.Equal(RoomForNewTaskResult.RoomForTask, test.RoomForNewTask);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [Fact]
        public void RoomForNewWorkGroup_Task_Disposed_Returns_False()
        {
            using (var test = Create())
            {
                test.Start();
                test.Dispose();
                Assert.Equal(RoomForNewTaskResult.No, test.RoomForNewWorkGroupTask(Substitute.For<IWorkGroup>()));
            }
        }

        [Theory, AutoData]
        public void RoomForNewWorkGroup_True(string value)
        {
            using (var test = Create())
            {
                test.Start();
                var group = test.AddWorkGroup(value, 1);
                Assert.Equal(RoomForNewTaskResult.RoomForTask, test.RoomForNewWorkGroupTask(group));
            }
        }

        [Fact]
        public void AddTask_Disposed_Exception()
        {
            using (var test = Create())
            {
                test.Start();
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
                    delegate
                    {
                        test.AddTask(new Task(() => { }));
                    });
            }
        }

        [Fact]
        public void AddTask()
        {
            using (var test = Create())
            {
                test.Start();
                test.AddTask(new Task(() => { Thread.Sleep(100); }));
            }
        }

        [Theory, AutoData]
        public void AddTask_WorkGroup(string value)
        {
            using (var test = Create())
            {
                test.Start();
                var group = test.AddWorkGroup(value, 1);
                var state = new StateInformation(group);
                var t = new Task(state1 => { Thread.Sleep(100); }, state);
                test.AddTask(t);
            }
        }

        [Fact]
        public void MaximumCurrencyLevel_1()
        {
            using (var test = Create())
            {
                test.Start();
                Assert.Equal(1, test.MaximumConcurrencyLevel);
            }
        }

        [Theory, AutoData]
        public void AddWorkGroup_Disposed_Exception(string value)
        {
            using (var test = Create())
            {
                test.Start();
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
                    delegate
                    {
                        test.AddWorkGroup(value, 1);
                    });
            }
        }

        [Theory, AutoData]
        public void AddWorkGroup_Disposed_Exception2(string value)
        {
            using (var test = Create())
            {
                test.Start();
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
                    delegate
                    {
                        test.AddWorkGroup(value, 1);
                    });
            }
        }

        [Theory, AutoData]
        public void AddWorkGroup(string value)
        {
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
