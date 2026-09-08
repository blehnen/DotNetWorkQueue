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

        [TestMethod]
        public async Task WaitAsync_NullGroup_RoutesToTheSharedWait()
        {
            //asserting only the initial signaled state would also pass for an
            //implementation that gave null its own private source, so drive it
            using (var test = CreateWithRealWait())
            {
                test.Reset(null);
                var waiter = test.WaitAsync(null).AsTask();
                Assert.IsFalse(waiter.IsCompleted, "must not complete while the shared wait is reset");

                test.Set(null);
                Assert.IsTrue(await waiter);
            }
        }

        [TestMethod]
        public void WaitAsync_NullGroup_NotReleasedByAGroupSet()
        {
            using (var test = CreateWithRealWait())
            {
                var group = Substitute.For<IWorkGroup>();

                test.Reset(null);
                var waiter = test.WaitAsync(null).AsTask();

                test.Set(group);

                Assert.IsFalse(waiter.IsCompleted, "a group Set released the shared waiter");
            }
        }

        [TestMethod]
        public async Task WaitAsync_Group_ReturnsAfterSet()
        {
            using (var test = CreateWithRealWait())
            {
                var group = Substitute.For<IWorkGroup>();

                test.Reset(group);
                var waiter = test.WaitAsync(group).AsTask();
                Assert.IsFalse(waiter.IsCompleted, "must not complete while the group is reset");

                test.Set(group);
                Assert.IsTrue(await waiter);
            }
        }

        [TestMethod]
        public async Task WaitAsync_GroupsAreIndependent()
        {
            //a full group must not release a waiter on a different group
            using (var test = CreateWithRealWait())
            {
                var groupOne = Substitute.For<IWorkGroup>();
                var groupTwo = Substitute.For<IWorkGroup>();

                test.Reset(groupOne);
                test.Reset(groupTwo);

                var waiterOne = test.WaitAsync(groupOne).AsTask();
                test.Set(groupTwo);

                Assert.IsFalse(waiterOne.IsCompleted, "setting another group released this one");

                test.Set(groupOne);
                Assert.IsTrue(await waiterOne);
            }
        }

        [TestMethod]
        public async Task WaitAsync_IfDisposed_Exception()
        {
            var test = CreateWithRealWait();
            test.Dispose();
            await Assert.ThrowsExactlyAsync<ObjectDisposedException>(
                async () => await test.WaitAsync(null));
        }

        private WaitForEventOrCancelThreadPool Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(fixture.Create<WaitForEventOrCancelFactory>());
            return fixture.Create<WaitForEventOrCancelThreadPool>();
        }

        //the async tests above assert on real signaled/unsignaled state, which the plain
        //AutoNSubstituteCustomization Create() helper can't provide - it auto-mocks
        //IWaitForEventOrCancelFactory.Create() into returning another substitute
        //(NSubstitute recursively mocks interface return values), not a working
        //WaitForEventOrCancel. Wire the factory to return real instances instead.
        private static WaitForEventOrCancelThreadPool CreateWithRealWait()
        {
            var factory = Substitute.For<IWaitForEventOrCancelFactory>();
            factory.Create().Returns(_ => new DotNetWorkQueue.Queue.WaitForEventOrCancel());
            return new WaitForEventOrCancelThreadPool(factory);
        }
    }
}
