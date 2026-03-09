using System.Threading;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;
using NSubstitute;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class AbortWorkerThreadTests
    {
        [TestMethod]
        public void If_Abort_Disabled_Return_Failure()
        {
            var test = Create(false);
            Assert.IsFalse(test.Abort(null));
        }
        [TestMethod]
        public void Abort_Allows_Null_Thread()
        {
            var test = Create(true);
            Assert.IsTrue(test.Abort(null));
        }

        [TestMethod]
        public void Abort_Allows_Stopped_Thread()
        {
            var test = Create(true);
            var t = new Thread(time => DoSomeWork(100000));
            Assert.IsTrue(test.Abort(t));
        }

#if NETFULL
        [TestMethod]
        public void Abort_ThreadFullFrameWork()
        {
            var test = Create(true);

            var t = new Thread((time) => DoSomeWork(100000));
            t.Start();

            Assert.IsTrue(test.Abort(t));
            Thread.Sleep(500);
            Assert.IsFalse(t.IsAlive);
        }
#endif

#if NETSTANDARD2_0
        [TestMethod]
        public void Abort_ThreadFullCore()
        {
            var test = Create(true);

            var t = new Thread(time => DoSomeWork(5000));
            t.Start();

            Assert.IsFalse(test.Abort(t));
            Thread.Sleep(1000);
            Assert.IsTrue(t.IsAlive);
            Thread.Sleep(5000);
            Assert.IsFalse(t.IsAlive);
        }
#endif
        private IAbortWorkerThread Create(bool enableAbort)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var configuration = fixture.Create<IWorkerConfiguration>();
            configuration.AbortWorkerThreadsWhenStopping.Returns(enableAbort);
            fixture.Inject(configuration);
            return fixture.Create<AbortWorkerThread>();
        }

        private void DoSomeWork(int time)
        {
            Thread.Sleep(time);
        }
    }
}
