// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Queue;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;
// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.Queue
{
    public class HeartBeatThreadPoolTests
    {
        [Fact]
        public void IsStarted_False_If_Start_Not_Called()
        {
            var test = Create();
            Assert.Equal(test.IsStarted, false);
        }

        [Fact]
        public void IsShuttingdown_False_By_Default()
        {
            var test = Create();
            Assert.Equal(test.IsShuttingdown, false);
        }

        [Fact]
        public void IsStarted_True_If_Start_Called()
        {
            var test = Create();
            test.Start();
            Assert.Equal(test.IsStarted, true);
        }

        [Fact]
        public void IsDisposed_False_By_Default()
        {
            var test = Create();
            Assert.Equal(test.IsDisposed, false);
        }

        [Fact]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var test = Create();
            test.Dispose();
            Assert.Equal(test.IsDisposed, true);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [Fact]
        public void Call_Dispose_Multiple_Times_Ok()
        {
            using (var test = Create())
            {
                test.Dispose();
            }
        }

        [Fact]
        public void Null_WorkItem_Exception()
        {
            using (var test = Create())
            {
                Assert.Throws<ArgumentNullException>(
               delegate
               {
                   test.QueueWorkItem(null);
               });
            }
        }

        [Fact]
        public void QueueWorkItem_After_Dispose_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
                    delegate
                    {
                        Action action = () => { };
                        test.QueueWorkItem(action);
                    });
            }
        }

        [Fact]
        public void QueueWorkItem_Start_Queue_First_Exception()
        {
            using (var test = Create(1, 1, TimeSpan.FromDays(1)))
            {
                Action action = () => { Thread.Sleep(3000); };
                Assert.Throws<DotNetWorkQueueException>(
                    delegate
                    {
                        test.QueueWorkItem(action);
                    });
            }
        }

        [Fact]
        public void QueueWorkItem_Work_Count_1()
        {
            using (var test = Create(1, 1, TimeSpan.FromDays(1)))
            {
                test.Start();
                Action action = () => { Thread.Sleep(3000); };
                test.QueueWorkItem(action);
                Thread.Sleep(1500);
                Assert.Equal(1, test.ActiveThreads);
            }
        }

        [Fact]
        public void QueueWorkItem_Work_Count_2()
        {
            using (var test = Create(2, 2, TimeSpan.FromDays(1)))
            {
                test.Start();
                Action action = () => { Thread.Sleep(3000); };
                test.QueueWorkItem(action);
                test.QueueWorkItem(action);
                Thread.Sleep(1500);
                Assert.Equal(2, test.ActiveThreads);
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

        private HeartBeatThreadPool Create(int threadsMax, int threadsMin, TimeSpan idle)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var configuration = fixture.Create<IHeartBeatThreadPoolConfiguration>();
            fixture.Inject(configuration);
            configuration.ThreadIdleTimeout.Returns(idle);
            configuration.ThreadsMax.Returns(threadsMax);
            configuration.ThreadsMin.Returns(threadsMin);
            return fixture.Create<HeartBeatThreadPool>();
        }

        private HeartBeatThreadPool Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var configuration = fixture.Create<IHeartBeatThreadPoolConfiguration>();
            fixture.Inject(configuration);
            return fixture.Create<HeartBeatThreadPool>();
        }
    }
}
