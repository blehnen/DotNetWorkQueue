using System;
using System.Threading;
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
    /// Covers both branches of the receive policy decorator, on both the synchronous and the
    /// asynchronous path: a pipeline registered for the key, and no pipeline registered.
    ///
    /// Neither path had tests before. The asynchronous one is the reason these exist — its
    /// cancellation token used to be dropped on the floor, so a retry or timeout strategy kept
    /// waiting out its delay after the queue had been told to stop.
    /// </summary>
    [TestClass]
    public class ReceiveMessagesPolicyDecoratorTests
    {
        private const string Key = "ReceiveMessageFromTransport";

        [TestMethod]
        public void ReceiveMessage_WithNoPipelineRegistered_CallsTheHandler()
        {
            var (decorator, handler, context) = Create(registerPipeline: false);
            var expected = Substitute.For<IReceivedMessageInternal>();
            handler.ReceiveMessage(context).Returns(expected);

            Assert.AreSame(expected, decorator.ReceiveMessage(context));
        }

        [TestMethod]
        public void ReceiveMessage_WithPipelineRegistered_CallsTheHandler()
        {
            var (decorator, handler, context) = Create(registerPipeline: true);
            var expected = Substitute.For<IReceivedMessageInternal>();
            handler.ReceiveMessage(context).Returns(expected);

            Assert.AreSame(expected, decorator.ReceiveMessage(context));
        }

        [TestMethod]
        public async Task ReceiveMessageAsync_WithNoPipelineRegistered_CallsTheHandler()
        {
            var (decorator, handler, context) = Create(registerPipeline: false);
            var expected = Substitute.For<IReceivedMessageInternal>();
            SetupAsync(handler, context, expected);

            Assert.AreSame(expected, await decorator.ReceiveMessageAsync(context, CancellationToken.None));
        }

        [TestMethod]
        public async Task ReceiveMessageAsync_WithPipelineRegistered_CallsTheHandler()
        {
            var (decorator, handler, context) = Create(registerPipeline: true);
            var expected = Substitute.For<IReceivedMessageInternal>();
            SetupAsync(handler, context, expected);

            Assert.AreSame(expected, await decorator.ReceiveMessageAsync(context, CancellationToken.None));
        }

        [TestMethod]
        public async Task ReceiveMessageAsync_GivesTheCancellationTokenToThePipeline()
        {
            var (decorator, handler, context) = Create(registerPipeline: true);
            SetupAsync(handler, context, Substitute.For<IReceivedMessageInternal>());

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            //An already-cancelled token must stop the pipeline before it reaches the handler. If the
            //token is not forwarded, the pipeline runs against CancellationToken.None, the handler is
            //called, and nothing throws - which is precisely the defect: a retry or timeout strategy
            //would go on waiting out its delay after the queue had been told to stop.
            await Assert.ThrowsAsync<OperationCanceledException>(
                async () => await decorator.ReceiveMessageAsync(context, cts.Token));

            await handler.DidNotReceive().ReceiveMessageAsync(context, Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public void IsBlockingOperation_DelegatesToInner()
        {
            var (decorator, handler, _) = Create(registerPipeline: false);
            handler.IsBlockingOperation.Returns(true);
            Assert.IsTrue(decorator.IsBlockingOperation);
        }

        private static void SetupAsync(IReceiveMessages handler, IMessageContext context,
            IReceivedMessageInternal result)
        {
            //Configuring a substitute never consumes the ValueTask, which is the one case CA2012's
            //rule does not fit.
#pragma warning disable CA2012
            handler.ReceiveMessageAsync(context, Arg.Any<CancellationToken>())
                .Returns(new ValueTask<IReceivedMessageInternal>(result));
#pragma warning restore CA2012
        }

        private static (IReceiveMessages decorator, IReceiveMessages handler, IMessageContext context)
            Create(bool registerPipeline)
        {
            var handler = Substitute.For<IReceiveMessages>();
            var policies = Substitute.For<IPolicies>();
            var registry = new ResiliencePipelineRegistry<string>();

            if (registerPipeline)
            {
                registry.GetOrAddPipeline(Key,
                    builder => builder.AddTimeout(TimeSpan.FromSeconds(30)));
            }

            policies.Registry.Returns(registry);
            policies.Definition.Returns(new PolicyDefinitions());

            return (new ReceiveMessagesPolicyDecorator(policies, handler), handler,
                Substitute.For<IMessageContext>());
        }
    }
}
