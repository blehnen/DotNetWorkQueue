using System;
using System.Threading.Tasks;
using DotNetWorkQueue.Policies;
using DotNetWorkQueue.Policies.Decorator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Polly.Registry;

namespace DotNetWorkQueue.Tests.Policies.Decorator
{
    /// <summary>
    /// Both branches of the heartbeat policy decorator, on both paths: a pipeline registered for the
    /// key, and none. The asynchronous member is new with #284's heartbeat work and had no test.
    /// </summary>
    [TestClass]
    public class SendHeartBeatPolicyDecoratorTests
    {
        private const string Key = "SendHeartBeat";

        [TestMethod]
        public void Send_WithNoPipelineRegistered_CallsTheHandler()
        {
            var (decorator, handler, context) = Create(registerPipeline: false);
            var expected = Substitute.For<IHeartBeatStatus>();
            handler.Send(context).Returns(expected);

            Assert.AreSame(expected, decorator.Send(context));
        }

        [TestMethod]
        public void Send_WithPipelineRegistered_CallsTheHandler()
        {
            var (decorator, handler, context) = Create(registerPipeline: true);
            var expected = Substitute.For<IHeartBeatStatus>();
            handler.Send(context).Returns(expected);

            Assert.AreSame(expected, decorator.Send(context));
        }

        [TestMethod]
        public async Task SendAsync_WithNoPipelineRegistered_CallsTheHandler()
        {
            var (decorator, handler, context) = Create(registerPipeline: false);
            var expected = Substitute.For<IHeartBeatStatus>();
            handler.SendAsync(context).Returns(Task.FromResult(expected));

            Assert.AreSame(expected, await decorator.SendAsync(context));
        }

        [TestMethod]
        public async Task SendAsync_WithPipelineRegistered_CallsTheHandler()
        {
            var (decorator, handler, context) = Create(registerPipeline: true);
            var expected = Substitute.For<IHeartBeatStatus>();
            handler.SendAsync(context).Returns(Task.FromResult(expected));

            //the result has to survive the pipeline, not just reach the handler
            Assert.AreSame(expected, await decorator.SendAsync(context));
            await handler.Received(1).SendAsync(context);
        }

        private static (ISendHeartBeat decorator, ISendHeartBeat handler, IMessageContext context)
            Create(bool registerPipeline)
        {
            var handler = Substitute.For<ISendHeartBeat>();
            var policies = Substitute.For<IPolicies>();
            var registry = new ResiliencePipelineRegistry<string>();

            if (registerPipeline)
            {
                registry.GetOrAddPipeline(Key,
                    builder => builder.AddTimeout(TimeSpan.FromSeconds(30)));
            }

            policies.Registry.Returns(registry);
            policies.Definition.Returns(new PolicyDefinitions());

            return (new SendHeartBeatPolicyDecorator(handler, policies), handler,
                Substitute.For<IMessageContext>());
        }
    }
}
