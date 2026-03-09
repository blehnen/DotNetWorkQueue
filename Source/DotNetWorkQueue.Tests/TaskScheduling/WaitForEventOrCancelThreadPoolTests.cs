using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;
using DotNetWorkQueue.TaskScheduling;
using NSubstitute;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.TaskScheduling
{
    [TestClass]
    public class WaitForEventOrCancelThreadPoolTests
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

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [TestMethod]
        public void Dispose_Can_Be_Called_Multiple_Times()
        {
            using (var test = Create())
            {
                test.Dispose();
            }
        }

        [TestMethod]
        public void Disposed_Wait_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.ThrowsExactly<ObjectDisposedException>(
            delegate
            {
                test.Wait(null);
            });
            }
        }

        [TestMethod]
        public void Disposed_Cancel_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.ThrowsExactly<ObjectDisposedException>(
            delegate
            {
                test.Cancel();
            });
            }
        }

        [TestMethod]
        public void Disposed_Reset_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.ThrowsExactly<ObjectDisposedException>(
            delegate
            {
                test.Reset(null);
            });
            }
        }

        [TestMethod]
        public void Disposed_Set_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.ThrowsExactly<ObjectDisposedException>(
            delegate
            {
                test.Set(null);
            });
            }
        }

        [TestMethod]
        public void Default()
        {
            using (var test = Create())
            {
                test.Wait(Substitute.For<IWorkGroup>());
                test.Cancel();
            }
        }

        [TestMethod]
        public void Default_Set()
        {
            using (var test = Create())
            {
                test.Set(null);
            }
        }

        [TestMethod]
        public void Default_Set_WorkGroup()
        {
            using (var test = Create())
            {
                test.Set(Substitute.For<IWorkGroup>());
            }
        }

        [TestMethod]
        public void Default_Wait()
        {
            using (var test = Create())
            {
                test.Wait(null);
            }
        }

        [TestMethod]
        public void Default_Wait_WorkGroup()
        {
            using (var test = Create())
            {
                test.Wait(Substitute.For<IWorkGroup>());
            }
        }


        [TestMethod]
        public void Default_Reset()
        {
            using (var test = Create())
            {
                test.Reset(null);
            }
        }

        [TestMethod]
        public void Default_Reset_WorkGroup()
        {
            using (var test = Create())
            {
                test.Reset(Substitute.For<IWorkGroup>());
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public async Task Default_WorkGroup_Threads()
        {
            using (var test = Create())
            {
                var group = Substitute.For<IWorkGroup>();
                group.Name.Returns("Test");

                var task1 = Task.Factory.StartNew(() => test.Wait(group), TaskCreationOptions.LongRunning);
                var task2 = Task.Factory.StartNew(() => test.Wait(group), TaskCreationOptions.LongRunning);
                var task3 = Task.Factory.StartNew(() => test.Wait(group), TaskCreationOptions.LongRunning);
                var task4 = Task.Factory.StartNew(() => test.Wait(group), TaskCreationOptions.LongRunning);
                var task5 = Task.Factory.StartNew(() => test.Wait(group), TaskCreationOptions.LongRunning);

                // Cancel must fire concurrently to unblock the Wait calls
                await Task.Delay(500);
                test.Cancel();

                await Task.WhenAll(task1, task2, task3, task4, task5);
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
