using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;
using DotNetWorkQueue.Queue;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class WorkerTests
    {
        [TestMethod]
        public void Start_Stop()
        {
            using (var worker = Create())
            {
                worker.Start();
                worker.Stop();
            }
        }

        [TestMethod]
        public void Start_Stop_Start()
        {
            using (var worker = Create())
            {
                worker.Start();
                worker.Stop();
                worker.Start();
            }
        }

        [TestMethod]
        public void Stop_without_Start_Ok()
        {
            using (var worker = Create())
            {
                worker.Stop();
            }
        }

        [TestMethod]
        public void Stop_Multiple_Ok()
        {
            using (var worker = Create())
            {
                worker.Stop();
                worker.Stop();
            }
        }

        [TestMethod]
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
