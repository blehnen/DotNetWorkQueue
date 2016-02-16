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
using DotNetWorkQueue.TaskScheduling;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;
using ThreadPool = DotNetWorkQueue.TaskScheduling.ThreadPool;
// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.TaskScheduling
{
    public class ThreadPoolTests
    {
        [Fact]
        public void IsDisposed_False_By_Default()
        {
            using (var test = Create())
            {
                Assert.Equal(test.IsDisposed, false);
            }
        }

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
        public void Dispose_Can_Be_Called_Multiple_Times()
        {
            using (var test = Create())
            {
                test.Dispose();
            }
        }

        [Fact]
        public void Default_IsShuttingDown_True_After_dispose()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.True(test.IsShuttingdown);
            }
        }

        [Fact]
        public void Default_ActiveThreads_0_After_dispose()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.Equal(0, test.ActiveThreads);
            }
        }

        [Fact]
        public void Default_Start_Dispose()
        {
            using (var test = Create())
            {
                test.Start();
                Assert.True(test.IsStarted);
            }
        }
         [Fact]
        public void Disposed_Instance_Set_Start_Exception()
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
         [Fact]
        public void Disposed_Instance_Queue_WorkItem_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.QueueWorkItem(() => { Thread.Sleep(10);   });
            });
            }
        }
         [Fact]
        public void Queue_WorkItem()
        {
            using (var test = Create())
            {
                test.Start();
                test.QueueWorkItem(() => { Thread.Sleep(10); });
            }
        }
         [Fact]
        public void Queue_WorkItem_Active_Threads_1()
        {
            using (var test = Create())
            {
                test.Start();
                test.QueueWorkItem(() => { Thread.Sleep(2000); });
                Thread.Sleep(500);
                Assert.Equal(1, test.ActiveThreads);
            }
        }


        private ThreadPool Create()
        {
            return Create(TimeSpan.FromSeconds(5), 1, 1, TimeSpan.FromSeconds(5));
        }

        private ThreadPool Create(TimeSpan idle, int max, int min, TimeSpan wait)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var config = fixture.Create<ThreadPoolConfiguration>();
            config.IdleTimeout = idle;
            config.MaxWorkerThreads = max;
            config.MinWorkerThreads = min;
            config.WaitForTheadPoolToFinish = wait;
            fixture.Inject(config);
            return fixture.Create<ThreadPool>();
        }
    }
}
