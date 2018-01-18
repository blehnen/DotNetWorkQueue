using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;
using Xunit;

// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.Queue
{
    public class WorkerWaitForEventOrCancelTests
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
        public void Cancel_IfDisposed_Exception()
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
        public void Reset_IfDisposed_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
                    delegate
                    {
                        test.Reset();
                    });
            }
        }

        [Fact]
        public void Wait_IfDisposed_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
                    delegate
                    {
                        test.Wait();
                    });
            }
        }
        [Fact]
        public void Set_IfDisposed_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
                    delegate
                    {
                        test.Set();
                    });
            }
        }

        [Fact]
        public void Wait_Set()
        {
            using (var test = Create())
            {
                test.Reset();
                Task.Factory.StartNew(() => { Thread.Sleep(1000); test.Set(); });
                test.Wait();
            }
        }

        [Fact]
        public void Wait_Cancel()
        {
            using (var test = Create())
            {
                test.Reset();
                Task.Factory.StartNew(() => { Thread.Sleep(1000); test.Cancel(); });
                test.Wait();
            }
        }

        [Fact]
        public void Wait_Set_Reset_Wait()
        {
            using (var test = Create())
            {
                test.Reset();
                Task.Factory.StartNew(() => { Thread.Sleep(1000); test.Set(); });
                test.Wait();
                test.Reset();
                Task.Factory.StartNew(() => { Thread.Sleep(1000); test.Set(); });
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
