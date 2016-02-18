// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.TaskScheduling;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Ploeh.AutoFixture.Xunit2;
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
                Assert.Equal(test.IsDisposed, false);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [Fact]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.Equal(test.IsDisposed, true);
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
        public void Calling_Start_Multiple_Times_Exception()
        {
            using (var test = Create())
            {
                test.Start();
                Assert.Throws<DotNetWorkQueueException>(
                    delegate
                    {
                        test.Start();
                    });
            }
        }

        [Fact]
        public void Calling_Start_With_Max_Zero_Exception()
        {
            using (var test = Create(0, 0, TimeSpan.MaxValue, TimeSpan.MaxValue))
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
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
            using (var test = Create())
            {
               test.Start();
               Assert.Equal(RoomForNewTaskResult.RoomForTask, test.RoomForNewTask);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [Fact]
        public void RoomForNewWorkgroup_Task_Disposed_Returns_False()
        {
            using (var test = Create())
            {
                test.Start(); 
                test.Dispose();
                Assert.Equal(RoomForNewTaskResult.No, test.RoomForNewWorkGroupTask(Substitute.For<IWorkGroup>()));
            }
        }

        [Theory, AutoData]
        public void RoomForNewWorkgroup_True(string value)
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
                test.AddTask(new Task(() => { Thread.Sleep(100);}));
            }
        }

        [Theory, AutoData]
        public void AddTask_Workgroup(string value)
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
                        test.AddWorkGroup(value, 1, 0);
                    });
            }
        }

        [Theory, AutoData]
        public void AddWorkGroup_NotStarted_Exception(string value)
        {
            using (var test = Create())
            {
                Assert.Throws<DotNetWorkQueueException>(
                    delegate
                    {
                        test.AddWorkGroup(value, 1, 0);
                    });
            }
        }

        [Theory, AutoData]
        public void AddWorkGroup(string value)
        {
            using (var test = Create())
            {
                test.Start();
                test.AddWorkGroup(value, 1, 0);
            }
        }


        private SmartThreadPoolTaskScheduler Create(int max, int min, TimeSpan idle, TimeSpan wait)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var config = fixture.Create<ITaskSchedulerConfiguration>();

            config.MaximumThreads.Returns(max);
            config.MaxQueueSize.Returns(0);
            config.MinimumThreads.Returns(min);
            config.ThreadIdleTimeout.Returns(idle);
            config.WaitForTheadPoolToFinish.Returns(wait);
            fixture.Inject(config);

            return fixture.Create<SmartThreadPoolTaskScheduler>();
        }

        private SmartThreadPoolTaskScheduler Create()
        {
            return Create(1, 1, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }
    }
}
