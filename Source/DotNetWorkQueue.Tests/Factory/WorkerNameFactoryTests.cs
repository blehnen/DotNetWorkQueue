using System.Collections.Concurrent;
using System.Threading;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;


using Xunit;

namespace DotNetWorkQueue.Tests.Factory
{
    public class WorkerNameFactoryTests
    {
        [Fact]
        public void Create_Default()
        {
            var factory = Create();
            Assert.NotNull(factory.Create());
        }
        [Fact]
        public void Create_Multi_Threaded()
        {
            const int numThreads = 25;

            var names = new ConcurrentDictionary<string, string>();

            var factory = Create();
            var resetEvent = new ManualResetEvent(false);
            var toProcess = numThreads;
            for (var i = 0; i < numThreads; i++)
            {
                new Thread(delegate()
                {
                    var name = factory.Create();
                    names.TryAdd(name, name);
                    if (Interlocked.Decrement(ref toProcess) == 0)
                        resetEvent.Set();
                }).Start();
            }
            resetEvent.WaitOne();
            Assert.Equal(names.Count, numThreads);
        }
        private IWorkerNameFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<WorkerNameFactory>();
        }
    }
}
