using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Queue;
using NSubstitute;


using Xunit;

// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.Queue
{
    public class WorkerCollectionTests
    {
        [Fact]
        public void Start_Multiple_Times_Fails()
        {
            using (var test = Create(2))
            {
                test.Start();
                Assert.Throws<DotNetWorkQueueException>(
               delegate
               {
                   test.Start();
               });
            }
        }

        [Fact]
        public void Stop_Multiple_Times_Ok()
        {
            using (var test = Create(2))
            {
                test.Start();
                test.Stop();
                test.Stop();
            }
        }

        [Fact]
        public void Stop_Without_Start_ok()
        {
            using (var test = Create(2))
            {
                test.Stop();
            }
        }

        private WorkerCollection Create(int workerCount)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var configuration = fixture.Create<IWorkerConfiguration>();
            configuration.WorkerCount.Returns(workerCount);
            fixture.Inject(configuration);

            return fixture.Create<WorkerCollection>();
        }
    }
}
