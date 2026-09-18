using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Policies;
using DotNetWorkQueue.Policies.Decorator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly.Registry;

namespace DotNetWorkQueue.Tests.Policies
{
    /// <summary>
    /// What the policy decorators do when the pipeline registry has already been disposed.
    /// </summary>
    /// <remarks>
    /// The registry is a container singleton, so the container disposes it. Shutdown does not wait
    /// indefinitely for in-flight work - StopWorkers waits out two configured timeouts and then force
    /// terminates - so a handler can still be running when that happens, and asking a disposed
    /// registry for a pipeline throws.
    ///
    /// #121 caught that in the transport retry decorators. These are the three in core that were left,
    /// and the receive one is on the path most likely to still be running (GitHub #135).
    ///
    /// No race is needed to test it: disposing the registry first reaches the same code by the same
    /// route, deterministically.
    /// </remarks>
    [TestClass]
    public class PolicyDecoratorShutdownRaceTests
    {
        [TestMethod]
        public void ReceiveMessage_WithADisposedRegistry_StillReceives()
        {
            var handler = Substitute.For<IReceiveMessages>();
            var expected = Substitute.For<IReceivedMessageInternal>();
            handler.ReceiveMessage(Arg.Any<IMessageContext>()).Returns(expected);

            var decorator = new ReceiveMessagesPolicyDecorator(DisposedPolicies(), handler);

            Assert.AreSame(expected, decorator.ReceiveMessage(Substitute.For<IMessageContext>()));
        }

        [TestMethod]
        public async Task ReceiveMessageAsync_WithADisposedRegistry_StillReceives()
        {
            var handler = Substitute.For<IReceiveMessages>();
            var expected = Substitute.For<IReceivedMessageInternal>();
            //CA2012 fires on a ValueTask passed as an argument, which is what an NSubstitute setup is
#pragma warning disable CA2012
            handler.ReceiveMessageAsync(Arg.Any<IMessageContext>(), Arg.Any<CancellationToken>())
                .Returns(new ValueTask<IReceivedMessageInternal>(expected));
#pragma warning restore CA2012

            var decorator = new ReceiveMessagesPolicyDecorator(DisposedPolicies(), handler);

            var actual = await decorator.ReceiveMessageAsync(Substitute.For<IMessageContext>(), CancellationToken.None);
            Assert.AreSame(expected, actual);
        }

        [TestMethod]
        public void SendHeartBeat_WithADisposedRegistry_StillSends()
        {
            var handler = Substitute.For<ISendHeartBeat>();
            var expected = Substitute.For<IHeartBeatStatus>();
            handler.Send(Arg.Any<IMessageContext>()).Returns(expected);

            var decorator = new SendHeartBeatPolicyDecorator(handler, DisposedPolicies());

            Assert.AreSame(expected, decorator.Send(Substitute.For<IMessageContext>()));
        }

        [TestMethod]
        public void SendMessage_WithADisposedRegistry_StillSends()
        {
            var handler = Substitute.For<ISendMessages>();
            var expected = Substitute.For<IQueueOutputMessage>();
            handler.Send(Arg.Any<IMessage>(), Arg.Any<IAdditionalMessageData>()).Returns(expected);

            var decorator = new SendMessagesPolicyDecorator(DisposedPolicies(), handler);

            Assert.AreSame(expected,
                decorator.Send(Substitute.For<IMessage>(), Substitute.For<IAdditionalMessageData>()));
        }

        /// <summary>
        /// Policies whose registry has been disposed, which is the state the container leaves behind
        /// when it is disposed while a handler is still running.
        /// </summary>
        private static IPolicies DisposedPolicies()
        {
            var registry = new ResiliencePipelineRegistry<string>();
            registry.Dispose();
            return new DotNetWorkQueue.Policies.Policies(registry, new PolicyDefinitions());
        }
    }
}
