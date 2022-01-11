using System;
using System.Diagnostics.CodeAnalysis;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Queue;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class BaseQueueTests
    {
        [Fact]
        public void IsDisposed_False_By_Default()
        {
            var test = CreateQueue();
            Assert.False(test.IsDisposed);
        }

        [Fact]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var test = CreateQueue();
            test.Dispose();
            Assert.True(test.IsDisposed);
        }

        [Fact]
        public void Disposed_Instance_Set_ShouldWork_Exception()
        {
            var test = CreateQueue();
            test.Dispose();
            Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.ShouldWorkPublic = true;
            });
        }

        [Fact]
        public void Disposed_Instance_Get_ShouldWork_NoException()
        {
            var test = CreateQueue();
            test.Dispose();
            Assert.False(test.ShouldWorkPublic);
        }

        [Fact]
        public void Disposed_Instance_Set_Started_Exception()
        {
            var test = CreateQueue();
            test.Dispose();
            Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.StartedPublic = true;
            });
        }

        [Fact]
        public void Disposed_Instance_Get_Started_NoException()
        {
            var test = CreateQueue();
            test.Dispose();
            Assert.False(test.StartedPublic);
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [Fact]
        public void Dispose_Can_Be_Called_Multiple_Times()
        {
            var test = CreateQueue();
            test.Dispose();
            test.Dispose();
        }

        [Fact]
        public void SetGet_Started()
        {
            var test = CreateQueue();
            Assert.False(test.StartedPublic);
            test.StartedPublic = true;
            Assert.True(test.StartedPublic);
        }

        [Fact]
        public void SetGet_ShouldWork()
        {
            var test = CreateQueue();
            Assert.False(test.ShouldWorkPublic);
            test.ShouldWorkPublic = true;
            Assert.True(test.ShouldWorkPublic);
        }

        private BaseQueueTest CreateQueue()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return new BaseQueueTest(fixture.Create<ILogger>());
        }
    }

    public class BaseQueueTest : BaseQueue
    {
        public BaseQueueTest(ILogger log): base(log)
        {
            
        }

        public bool ShouldWorkPublic
        {
            get => ShouldWork;
            set => ShouldWork = value;
        }
        public bool StartedPublic
        {
            get => Started;
            set => Started = value;
        }
    }
}
