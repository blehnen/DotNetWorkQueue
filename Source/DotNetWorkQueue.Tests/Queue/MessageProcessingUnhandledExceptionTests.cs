using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Notifications;
using DotNetWorkQueue.Queue;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace DotNetWorkQueue.Tests.Queue
{
    /// <summary>
    /// An exception that none of the specific catches handle, here one thrown by the poison handler
    /// itself, is logged and reported before the consumer backs off (GitHub #444).
    /// </summary>
    /// <remarks>
    /// It used to reach a catch that only backed off: no log, no notification, so the message's fate was
    /// invisible even at Debug level.
    /// </remarks>
    [TestClass]
    public class MessageProcessingUnhandledExceptionTests
    {
        [TestMethod]
        public async Task Sync_A_Failing_Poison_Handler_Is_Logged_And_Reported()
        {
            var failure = new InvalidOperationException("the move to the error queue failed");
            var harness = new Harness(failure);
            harness.Receive.ReceiveMessage(Arg.Any<IMessageContext>()).Throws(new PoisonMessageException());
            harness.PoisonHandler
                .When(x => x.Handle(Arg.Any<IMessageContext>(), Arg.Any<PoisonMessageException>()))
                .Do(_ => throw failure);

            await harness.Fixture.Create<MessageProcessing>().HandleAsync().ConfigureAwait(false);

            harness.AssertLoggedAndReported(failure);
        }

        [TestMethod]
        public async Task Async_A_Failing_Poison_Handler_Is_Logged_And_Reported()
        {
            var failure = new InvalidOperationException("the move to the error queue failed");
            var harness = new Harness(failure);
            //configuring the substitute is the one place the returned ValueTask is not meant to be
            //consumed, and CA2012 is an error repo-wide
#pragma warning disable CA2012
            harness.Receive.ReceiveMessageAsync(Arg.Any<IMessageContext>(), Arg.Any<CancellationToken>())
                .Throws(new PoisonMessageException());
#pragma warning restore CA2012
            harness.PoisonHandler.HandleAsync(Arg.Any<IMessageContext>(), Arg.Any<PoisonMessageException>())
                .Returns(Task.FromException(failure));

            await harness.Fixture.Create<MessageProcessingAsync>().HandleAsync().ConfigureAwait(false);

            harness.AssertLoggedAndReported(failure);
        }

        [TestMethod]
        public async Task Sync_A_Throwing_Subscriber_Does_Not_Skip_The_Backoff()
        {
            //the error callback is user code; its own exception used to skip the backoff, and a second
            //throw from the outer catch's retry of it would end the worker
            var failure = new InvalidOperationException("the move to the error queue failed");
            var subscriberError = new InvalidOperationException("the subscriber failed");
            var harness = new Harness(failure);
            harness.Receive.ReceiveMessage(Arg.Any<IMessageContext>()).Throws(new PoisonMessageException());
            harness.PoisonHandler
                .When(x => x.Handle(Arg.Any<IMessageContext>(), Arg.Any<PoisonMessageException>()))
                .Do(_ => throw failure);
            harness.SubscriberThrows(subscriberError);

            await harness.Fixture.Create<MessageProcessing>().HandleAsync().ConfigureAwait(false);

            harness.FatalErrorDelay.Received(1).Wait();
            harness.AssertBothLogged(failure, subscriberError);
        }

        [TestMethod]
        public async Task Async_A_Throwing_Subscriber_Does_Not_Skip_The_Backoff()
        {
            var failure = new InvalidOperationException("the move to the error queue failed");
            var subscriberError = new InvalidOperationException("the subscriber failed");
            var harness = new Harness(failure);
#pragma warning disable CA2012
            harness.Receive.ReceiveMessageAsync(Arg.Any<IMessageContext>(), Arg.Any<CancellationToken>())
                .Throws(new PoisonMessageException());
#pragma warning restore CA2012
            harness.PoisonHandler.HandleAsync(Arg.Any<IMessageContext>(), Arg.Any<PoisonMessageException>())
                .Returns(Task.FromException(failure));
            harness.SubscriberThrows(subscriberError);

            await harness.Fixture.Create<MessageProcessingAsync>().HandleAsync().ConfigureAwait(false);

#pragma warning disable CA2012
            await harness.FatalErrorDelay.Received(1).WaitAsync().ConfigureAwait(false);
#pragma warning restore CA2012
            harness.AssertBothLogged(failure, subscriberError);
        }

        [TestMethod]
        public async Task Sync_A_Throwing_Logger_Does_Not_Skip_The_Backoff()
        {
            //Microsoft.Extensions.Logging rethrows a provider's exception from the Log call
            var failure = new InvalidOperationException("the move to the error queue failed");
            var harness = new Harness(failure);
            harness.Receive.ReceiveMessage(Arg.Any<IMessageContext>()).Throws(new PoisonMessageException());
            harness.PoisonHandler
                .When(x => x.Handle(Arg.Any<IMessageContext>(), Arg.Any<PoisonMessageException>()))
                .Do(_ => throw failure);
            harness.LoggerThrowsOn("back off");

            await harness.Fixture.Create<MessageProcessing>().HandleAsync().ConfigureAwait(false);

            harness.FatalErrorDelay.Received(1).Wait();
        }

        [TestMethod]
        public async Task Async_A_Throwing_Logger_Does_Not_Skip_The_Backoff()
        {
            var failure = new InvalidOperationException("the move to the error queue failed");
            var harness = new Harness(failure);
#pragma warning disable CA2012
            harness.Receive.ReceiveMessageAsync(Arg.Any<IMessageContext>(), Arg.Any<CancellationToken>())
                .Throws(new PoisonMessageException());
#pragma warning restore CA2012
            harness.PoisonHandler.HandleAsync(Arg.Any<IMessageContext>(), Arg.Any<PoisonMessageException>())
                .Returns(Task.FromException(failure));
            harness.LoggerThrowsOn("back off");

            await harness.Fixture.Create<MessageProcessingAsync>().HandleAsync().ConfigureAwait(false);

#pragma warning disable CA2012
            await harness.FatalErrorDelay.Received(1).WaitAsync().ConfigureAwait(false);
#pragma warning restore CA2012
        }

        private sealed class Harness
        {
            private readonly CapturingLogger _log = new CapturingLogger();

            public Harness(Exception failure)
            {
                Fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
                Receive = Substitute.For<IReceiveMessages>();
                var receiveFactory = Substitute.For<IReceiveMessagesFactory>();
                receiveFactory.Create().Returns(Receive);
                PoisonHandler = Substitute.For<IReceivePoisonMessage>();
                ErrorNotification = Substitute.For<IConsumerQueueErrorNotification>();
                FatalErrorDelay = Substitute.For<IQueueWait>();
                var waitFactory = Substitute.For<IQueueWaitFactory>();
                waitFactory.CreateFatalErrorDelay().Returns(FatalErrorDelay);

                Fixture.Inject(waitFactory);
                Fixture.Inject(receiveFactory);
                Fixture.Inject(PoisonHandler);
                Fixture.Inject(ErrorNotification);
                Fixture.Inject<ILogger>(_log);
            }

            public IFixture Fixture { get; }
            public IReceiveMessages Receive { get; }
            public IReceivePoisonMessage PoisonHandler { get; }
            public IConsumerQueueErrorNotification ErrorNotification { get; }
            public IQueueWait FatalErrorDelay { get; }

            public void LoggerThrowsOn(string messagePart) => _log.ThrowOn = messagePart;

            public void SubscriberThrows(Exception subscriberError)
            {
                ErrorNotification.When(x => x.InvokeError(Arg.Any<ErrorReceiveNotification>()))
                    .Do(_ => throw subscriberError);
            }

            public void AssertBothLogged(Exception failure, Exception subscriberError)
            {
                Assert.HasCount(1, _log.Entries.Where(e => e.Level == LogLevel.Error && ReferenceEquals(e.Exception, failure)));
                Assert.HasCount(1, _log.Entries.Where(e => e.Level == LogLevel.Error && ReferenceEquals(e.Exception, subscriberError)));
            }

            public void AssertLoggedAndReported(Exception failure)
            {
                var logged = _log.Entries.Where(e => e.Level == LogLevel.Error && ReferenceEquals(e.Exception, failure)).ToList();
                Assert.HasCount(1, logged, "the failure should be logged once, at Error");
                StringAssert.Contains(logged[0].Message, "back off");

                ErrorNotification.Received(1).InvokeError(
                    Arg.Is<ErrorReceiveNotification>(n => ReferenceEquals(n.Error, failure)));
            }
        }

        private sealed class CapturingLogger : ILogger
        {
            private readonly ConcurrentQueue<(LogLevel Level, Exception Exception, string Message)> _entries = new();

            public (LogLevel Level, Exception Exception, string Message)[] Entries => _entries.ToArray();

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            /// <summary>When set, logging a message containing this text throws, as a failing provider does.</summary>
            public string ThrowOn { get; set; }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter)
            {
                var message = formatter(state, exception);
                if (ThrowOn != null && message.Contains(ThrowOn))
                    throw new InvalidOperationException("the log provider failed");
                _entries.Enqueue((logLevel, exception, message));
            }
        }
    }
}
