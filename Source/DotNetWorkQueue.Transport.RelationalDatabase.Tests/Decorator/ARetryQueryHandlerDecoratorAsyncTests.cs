using System.Threading.Tasks;
using DotNetWorkQueue.Transport.RelationalDatabase.Decorator;
using DotNetWorkQueue.Transport.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Polly.Registry;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Decorator
{
    /// <summary>
    /// The behaviour is tested once here rather than per transport: SQL Server, PostgreSQL and SQLite
    /// each supply nothing but the pipeline name, and that the decorator resolves at all is covered by
    /// container verification in their own test projects.
    /// </summary>
    [TestClass]
    public class ARetryQueryHandlerDecoratorAsyncTests
    {
        private const string PolicyKey = "TestRetryQueryHandler";

        public sealed class FakeQuery : IQuery<string> { }

        private sealed class Decorator : ARetryQueryHandlerDecoratorAsync<FakeQuery, string>
        {
            public Decorator(IQueryHandlerAsync<FakeQuery, string> decorated, IPolicies policies)
                : base(decorated, policies)
            {
            }

            protected override string PolicyName => PolicyKey;
        }

        [TestMethod]
        public async Task HandleAsync_WhenRegistryDisposed_FallsThroughToDecorated()
        {
            var decorated = Substitute.For<IQueryHandlerAsync<FakeQuery, string>>();
            decorated.HandleAsync(Arg.Any<FakeQuery>()).Returns("ok");
            var policies = Substitute.For<IPolicies>();
            var registry = new ResiliencePipelineRegistry<string>();
            registry.Dispose();
            policies.Registry.Returns(registry);

            var sut = new Decorator(decorated, policies);
            var q = new FakeQuery();

            var result = await sut.HandleAsync(q);

            Assert.AreEqual("ok", result);
            await decorated.Received(1).HandleAsync(q);
        }

        [TestMethod]
        public async Task HandleAsync_WhenPipelineRegistered_ExecutesThroughPipeline()
        {
            var decorated = Substitute.For<IQueryHandlerAsync<FakeQuery, string>>();
            decorated.HandleAsync(Arg.Any<FakeQuery>()).Returns("ok");
            var policies = Substitute.For<IPolicies>();
            var registry = new ResiliencePipelineRegistry<string>();
            var built = 0;
            registry.TryAddBuilder(PolicyKey, (_, _) => built++);
            policies.Registry.Returns(registry);

            var sut = new Decorator(decorated, policies);
            var q = new FakeQuery();

            var result = await sut.HandleAsync(q);

            Assert.AreEqual("ok", result);
            //the pipeline the subclass named is the one that ran, which is the part each transport relies on
            Assert.AreEqual(1, built);
            await decorated.Received(1).HandleAsync(q);
            registry.Dispose();
        }

        [TestMethod]
        public async Task HandleAsync_WhenNoPipelineRegistered_CallsDecoratedDirectly()
        {
            var decorated = Substitute.For<IQueryHandlerAsync<FakeQuery, string>>();
            decorated.HandleAsync(Arg.Any<FakeQuery>()).Returns("ok");
            var policies = Substitute.For<IPolicies>();
            var registry = new ResiliencePipelineRegistry<string>();
            policies.Registry.Returns(registry);

            var sut = new Decorator(decorated, policies);
            var q = new FakeQuery();

            var result = await sut.HandleAsync(q);

            Assert.AreEqual("ok", result);
            await decorated.Received(1).HandleAsync(q);
            registry.Dispose();
        }
    }
}
