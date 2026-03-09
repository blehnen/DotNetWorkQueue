using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class WorkerWaitForEventOrCancelTests
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
        public void Cancel_IfDisposed_Exception()
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
        public void Reset_IfDisposed_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.ThrowsExactly<ObjectDisposedException>(
                    delegate
                    {
                        test.Reset();
                    });
            }
        }

        [TestMethod]
        public void Wait_IfDisposed_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.ThrowsExactly<ObjectDisposedException>(
                    delegate
                    {
                        test.Wait();
                    });
            }
        }
        [TestMethod]
        public void Set_IfDisposed_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.ThrowsExactly<ObjectDisposedException>(
                    delegate
                    {
                        test.Set();
                    });
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void Wait_Set()
        {
            using (var test = Create())
            {
                test.Reset();
                Task.Factory.StartNew(() => { Thread.Sleep(1000); test.Set(); }, TaskCreationOptions.LongRunning);
                test.Wait();
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void Wait_Cancel()
        {
            using (var test = Create())
            {
                test.Reset();
                Task.Factory.StartNew(() => { Thread.Sleep(1000); test.Cancel(); }, TaskCreationOptions.LongRunning);
                test.Wait();
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void Wait_Set_Reset_Wait()
        {
            using (var test = Create())
            {
                test.Reset();
                Task.Factory.StartNew(() => { Thread.Sleep(1000); test.Set(); }, TaskCreationOptions.LongRunning);
                test.Wait();
                test.Reset();
                Task.Factory.StartNew(() => { Thread.Sleep(1000); test.Set(); }, TaskCreationOptions.LongRunning);
                test.Wait();
            }
        }

        private IWorkerWaitForEventOrCancel Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<WorkerWaitForEventOrCancel>();
        }
    }
}
