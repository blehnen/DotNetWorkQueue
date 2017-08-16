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
using System.Threading.Tasks;
using DotNetWorkQueue.Factory;
using DotNetWorkQueue.TaskScheduling;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;
// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.TaskScheduling
{
    public class WaitForEventOrCancelThreadPoolTests
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [Fact]
        public void Dispose_Can_Be_Called_Multiple_Times()
        {
            using (var test = Create())
            {
                test.Dispose();
            }
        }

        [Fact]
        public void Disposed_Wait_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.Wait(null);
            });
            }
        }

        [Fact]
        public void Disposed_Cancel_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.Cancel();
            });
            }
        }

        [Fact]
        public void Disposed_Reset_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.Reset(null);
            });
            }
        }

        [Fact]
        public void Disposed_Set_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.Set(null);
            });
            }
        }

        [Fact]
        public void Default()
        {
            using (var test = Create())
            {
                test.Wait(Substitute.For<IWorkGroup>());
                test.Cancel();
            }
        }

        [Fact]
        public void Default_Set()
        {
            using (var test = Create())
            {
                test.Set(null);
            }
        }

        [Fact]
        public void Default_Set_WorkGroup()
        {
            using (var test = Create())
            {
                test.Set(Substitute.For<IWorkGroup>());
            }
        }

        [Fact]
        public void Default_Wait()
        {
            using (var test = Create())
            {
                test.Wait(null);
            }
        }

        [Fact]
        public void Default_Wait_WorkGroup()
        {
            using (var test = Create())
            {
                test.Wait(Substitute.For<IWorkGroup>());
            }
        }


        [Fact]
        public void Default_Reset()
        {
            using (var test = Create())
            {
                test.Reset(null);
            }
        }

        [Fact]
        public void Default_Reset_WorkGroup()
        {
            using (var test = Create())
            {
                test.Reset(Substitute.For<IWorkGroup>());
            }
        }

        [Fact]
        public void Default_WorkGroup_Threads()
        {
            using (var test = Create())
            {
                var group = Substitute.For<IWorkGroup>();
                group.Name.Returns("Test");

                var task1 = new Task(() => test.Wait(group));
                var task2 = new Task(() => test.Wait(group));
                var task3 = new Task(() => test.Wait(group));
                var task4 = new Task(() => test.Wait(group));
                var task5 = new Task(() => test.Wait(group));

                task1.Start();
                task2.Start();
                task3.Start();
                task4.Start();
                task5.Start();


                Task.WaitAll(task1, task2, task3, task4, task5);

                test.Cancel();
            }
        }

        private WaitForEventOrCancelThreadPool Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(fixture.Create<WaitForEventOrCancelFactory>());
            return fixture.Create<WaitForEventOrCancelThreadPool>();
        }
    }
}
