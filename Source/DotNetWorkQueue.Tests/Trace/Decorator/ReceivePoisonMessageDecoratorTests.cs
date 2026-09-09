using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Trace.Decorator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests.Trace.Decorator
{
    /// <summary>
    /// The tracing decorator over the poison-message handler.
    ///
    /// It is the only one of the three decorators on <see cref="IReceivePoisonMessage"/> that is
    /// public; the logging and metrics ones are internal and are covered where the container builds
    /// the whole chain. What matters here is that the asynchronous member is a decorator rather than
    /// a bypass: it must reach the decorated handler, and it must not close the activity until the
    /// move to the error queue has finished, or the span records a duration that excludes the work
    /// it is meant to measure.
    /// </summary>
    [TestClass]
    public class ReceivePoisonMessageDecoratorTests
    {
        [TestMethod]
        public async Task HandleAsync_ReachesTheDecoratedHandler_WithNoTraceHeader()
        {
            var harness = new Harness(header: null);

            await harness.Decorator.HandleAsync(harness.Context, harness.Exception).ConfigureAwait(false);

            await harness.Decorated.Received(1).HandleAsync(harness.Context, harness.Exception)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task HandleAsync_ReachesTheDecoratedHandler_WithATraceHeader()
        {
            var harness = new Harness(header: new Dictionary<string, object>());

            await harness.Decorator.HandleAsync(harness.Context, harness.Exception).ConfigureAwait(false);

            await harness.Decorated.Received(1).HandleAsync(harness.Context, harness.Exception)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task HandleAsync_DoesNotCallTheBlockingHandler()
        {
            var harness = new Harness(header: null);

            await harness.Decorator.HandleAsync(harness.Context, harness.Exception).ConfigureAwait(false);

            harness.Decorated.DidNotReceive().Handle(Arg.Any<IMessageContext>(), Arg.Any<PoisonMessageException>());
        }

        [TestMethod]
        public async Task HandleAsync_WaitsForTheDecoratedHandler()
        {
            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var harness = new Harness(header: null);
            harness.Decorated.HandleAsync(Arg.Any<IMessageContext>(), Arg.Any<PoisonMessageException>())
                .Returns(_ => gate.Task);

            var handling = harness.Decorator.HandleAsync(harness.Context, harness.Exception);

            var completedEarly = await Task.WhenAny(handling, Task.Delay(TimeSpan.FromMilliseconds(250)))
                .ConfigureAwait(false) == handling;

            Assert.IsFalse(completedEarly,
                "The decorator returned before the handler it decorates had finished. The activity " +
                "would close over none of the work it is timing.");

            gate.TrySetResult(true);
            await handling.ConfigureAwait(false);
        }

        private sealed class Harness
        {
            public ReceivePoisonMessageDecorator Decorator { get; }
            public IReceivePoisonMessage Decorated { get; }
            public IMessageContext Context { get; }
            public PoisonMessageException Exception { get; }

            public Harness(IDictionary<string, object> header)
            {
                Decorated = Substitute.For<IReceivePoisonMessage>();
                Context = Substitute.For<IMessageContext>();
                Exception = new PoisonMessageException();

                var getHeader = Substitute.For<IGetHeader>();
                getHeader.GetHeaders(Arg.Any<IMessageId>()).Returns(header);

                Decorator = new ReceivePoisonMessageDecorator(Decorated,
                    new ActivitySource("DotNetWorkQueue.Tests.PoisonMessage"),
                    Substitute.For<IStandardHeaders>(),
                    getHeader);
            }
        }
    }
}
