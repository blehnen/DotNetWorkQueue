using System.Threading;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;
using NSubstitute;


using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class AbortWorkerThreadTests
    {
        [Fact]
        public void If_Abort_Disabled_Return_Failure()
        {
            var test = Create(false);
            Assert.False(test.Abort(null));
        }
        [Fact]
        public void Abort_Allows_Null_Thread()
        {
            var test = Create(true);
            Assert.True(test.Abort(null));
        }

        [Fact]
        public void Abort_Allows_Stopped_Thread()
        {
            var test = Create(true);
            var t = new Thread(time => DoSomeWork(100000));
            Assert.True(test.Abort(t));
        }

#if NETFULL
        [Fact]
        public void Abort_ThreadFullFrameWork()
        {
            var test = Create(true);

            var t = new Thread((time) => DoSomeWork(100000));
            t.Start();

            Assert.True(test.Abort(t));
            Thread.Sleep(500);
            Assert.False(t.IsAlive);
        }
#endif

#if NETSTANDARD2_0
        [Fact]
        public void Abort_ThreadFullCore()
        {
            var test = Create(true);

            var t = new Thread(time => DoSomeWork(5000));
            t.Start();

            Assert.False(test.Abort(t));
            Thread.Sleep(1000);
            Assert.True(t.IsAlive);
            Thread.Sleep(5000);
            Assert.False(t.IsAlive);
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
