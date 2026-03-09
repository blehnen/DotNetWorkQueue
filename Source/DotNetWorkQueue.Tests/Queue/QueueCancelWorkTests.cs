using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;


using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class QueueCancelWorkTests
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
        [TestMethod]
        public void Stop_Disposed_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.ThrowsExactly<ObjectDisposedException>(
                delegate
                {
                    test.StopTokenSource.Cancel();
                });
            }
        }
        [TestMethod]
        public void Cancel_Disposed_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.ThrowsExactly<ObjectDisposedException>(
                delegate
                {
                    test.CancellationTokenSource.Cancel();
                });
            }
        }
        [TestMethod]
        public void StopToken_Disposed_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.ThrowsExactly<ObjectDisposedException>(
                delegate
                {
                    test.StopWorkToken.ThrowIfCancellationRequested();
                });
            }
        }
        [TestMethod]
        public void CancelToken_Disposed_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.ThrowsExactly<ObjectDisposedException>(
                delegate
                {
                    test.CancelWorkToken.ThrowIfCancellationRequested();
                });
            }
        }
        [TestMethod]
        public void Tokens_Disposed_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.ThrowsExactly<ObjectDisposedException>(
                delegate
                {
                    var temp = test.Tokens;
                    Assert.IsNull(temp);
                });
            }
        }

        [TestMethod]
        public void Stop_Sets_Token()
        {
            using (var test = Create())
            {
                test.StopTokenSource.Cancel();
                Assert.IsTrue(test.StopWorkToken.IsCancellationRequested);
            }
        }
        [TestMethod]
        public void Cancel_Sets_Token()
        {
            using (var test = Create())
            {
                test.CancellationTokenSource.Cancel();
                Assert.IsTrue(test.CancelWorkToken.IsCancellationRequested);
            }
        }
        [TestMethod]
        public void Tokens_Contains_All_Tokens()
        {
            using (var test = Create())
            {
                CollectionAssert.Contains(test.Tokens.ToList(), test.CancelWorkToken);
                CollectionAssert.Contains(test.Tokens.ToList(), test.StopWorkToken);
            }
        }

        private IQueueCancelWork Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<QueueCancelWork>();
        }
    }
}
