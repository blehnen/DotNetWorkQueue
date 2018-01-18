using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;
using DotNetWorkQueue.Queue;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class WorkerTests
    {
        [Fact]
        public void Start_Stop()
        {
            using (var worker = Create())
            {
                worker.Start();
                worker.Stop();
            }
        }

        [Fact]
        public void Start_Stop_Start()
        {
            using (var worker = Create())
            {
                worker.Start();
                worker.Stop();
                worker.Start();
            }
        }

        [Fact]
        public void Stop_without_Start_Ok()
        {
            using (var worker = Create())
            {
                worker.Stop();
            }
        }

        [Fact]
        public void Stop_Multiple_Ok()
        {
            using (var worker = Create())
            {
                worker.Stop();
                worker.Stop();
            }
        }

        [Fact]
        public void Start_Multiple_Ok()
        {
            using (var worker = Create())
            {
                worker.Start();
                worker.Start();
            }
        }

        private IWorker Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var container = fixture.Create<IContainer>();
            fixture.Inject(container);
            var fact = fixture.Create<MessageProcessingFactory>();
            var wrapper = new MessageProcessingTests.MessageProcessingWrapper();
            var processing = wrapper.Create();
            fact.Create().Returns(processing);
            fixture.Inject(fact);
            return fixture.Create<Worker>();
        }
    }
}
