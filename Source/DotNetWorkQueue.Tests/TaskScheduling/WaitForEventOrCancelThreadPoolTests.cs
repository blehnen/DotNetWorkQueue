using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;
using DotNetWorkQueue.TaskScheduling;
using NSubstitute;
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

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
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
