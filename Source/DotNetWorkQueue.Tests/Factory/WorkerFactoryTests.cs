using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;
using DotNetWorkQueue.Queue;
using NSubstitute;


using Xunit;

namespace DotNetWorkQueue.Tests.Factory
{
    public class WorkerFactoryTests
    {
        [Fact]
        public void Create_Default()
        {
            var factory = Create();
            Assert.NotNull(factory.Create());
        }
        private IWorkerFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var container = fixture.Create<IContainer>();
            var mode = fixture.Create<MessageProcessingMode>();
            container.GetInstance<MessageProcessingMode>().Returns(mode);
            fixture.Inject(container);
            return fixture.Create<WorkerFactory>();
        }
    }
}
