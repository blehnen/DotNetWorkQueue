using System;
using System.Diagnostics.CodeAnalysis;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;


using Xunit;

// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.Queue
{
    public class QueueCancelWorkTests
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
        [Fact]
        public void Stop_Disposed_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
                delegate
                {
                    test.StopTokenSource.Cancel();
                });
            }
        }
        [Fact]
        public void Cancel_Disposed_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
                delegate
                {
                    test.CancellationTokenSource.Cancel();
                });
            }
        }
        [Fact]
        public void StopToken_Disposed_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
                delegate
                {
                    test.StopWorkToken.ThrowIfCancellationRequested();
                });
            }
        }
        [Fact]
        public void CancelToken_Disposed_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
                delegate
                {
                    test.CancelWorkToken.ThrowIfCancellationRequested();
                });
            }
        }
        [Fact]
        public void Tokens_Disposed_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
                delegate
                {
                    var temp = test.Tokens;
                    Assert.Null(temp);
                });
            }
        }

        [Fact]
        public void Stop_Sets_Token()
        {
            using (var test = Create())
            {
                test.StopTokenSource.Cancel();
                Assert.True(test.StopWorkToken.IsCancellationRequested);
            }
        }
        [Fact]
        public void Cancel_Sets_Token()
        {
            using (var test = Create())
            {
                test.CancellationTokenSource.Cancel();
                Assert.True(test.CancelWorkToken.IsCancellationRequested);
            }
        }
        [Fact]
        public void Tokens_Contains_All_Tokens()
        {
            using (var test = Create())
            {
                Assert.Contains(test.CancelWorkToken, test.Tokens);
                Assert.Contains(test.StopWorkToken, test.Tokens);
            }
        }

        private IQueueCancelWork Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<QueueCancelWork>();
        }
    }
}
