using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class BaseQueueTests
    {
        [TestMethod]
        public void IsDisposed_False_By_Default()
        {
            var test = CreateQueue();
            Assert.IsFalse(test.IsDisposed);
        }

        [TestMethod]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var test = CreateQueue();
            test.Dispose();
            Assert.IsTrue(test.IsDisposed);
        }

        [TestMethod]
        public void Disposed_Instance_Set_ShouldWork_Exception()
        {
            var test = CreateQueue();
            test.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(
            delegate
            {
                test.ShouldWorkPublic = true;
            });
        }

        [TestMethod]
        public void Disposed_Instance_Get_ShouldWork_NoException()
        {
            var test = CreateQueue();
            test.Dispose();
            Assert.IsFalse(test.ShouldWorkPublic);
        }

        [TestMethod]
        public void Disposed_Instance_Set_Started_Exception()
        {
            var test = CreateQueue();
            test.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(
            delegate
            {
                test.StartedPublic = true;
            });
        }

        [TestMethod]
        public void Disposed_Instance_Get_Started_NoException()
        {
            var test = CreateQueue();
            test.Dispose();
            Assert.IsFalse(test.StartedPublic);
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [TestMethod]
        public void Dispose_Can_Be_Called_Multiple_Times()
        {
            var test = CreateQueue();
            test.Dispose();
            test.Dispose();
        }

        [TestMethod]
        public void SetGet_Started()
        {
            var test = CreateQueue();
            Assert.IsFalse(test.StartedPublic);
            test.StartedPublic = true;
            Assert.IsTrue(test.StartedPublic);
        }

        [TestMethod]
        public void SetGet_ShouldWork()
        {
            var test = CreateQueue();
            Assert.IsFalse(test.ShouldWorkPublic);
            test.ShouldWorkPublic = true;
            Assert.IsTrue(test.ShouldWorkPublic);
        }

        private BaseQueueTest CreateQueue()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return new BaseQueueTest(fixture.Create<ILogger>(), fixture.Create<IConsumerQueueErrorNotification>(), fixture.Create<IConsumerQueueNotification>());
        }
    }

    public class BaseQueueTest : BaseQueue
    {
        public BaseQueueTest(ILogger log, IConsumerQueueErrorNotification errors, IConsumerQueueNotification notify) : base(log, notify, errors)
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
