using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.History.Decorator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests.History.Decorator
{
    [TestClass]
    public class ReceiveMessagesHistoryDecoratorTests
    {
        [TestMethod]
        public void ReceiveMessage_Calls_Inner_Handler_And_Returns_Result()
        {
            var (decorator, inner, _, _, _) = CreateDecorator(enabled: false);
            var context = CreateContext();
            var expectedResult = Substitute.For<IReceivedMessageInternal>();
            inner.ReceiveMessage(context).Returns(expectedResult);

            var result = decorator.ReceiveMessage(context);

            Assert.AreSame(expectedResult, result);
            inner.Received(1).ReceiveMessage(context);
        }

        [TestMethod]
        public void ReceiveMessage_When_Enabled_Records_Processing_Start()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: true, trackProcessing: true);
            var context = CreateContext();
            var receivedMessage = Substitute.For<IReceivedMessageInternal>();
            inner.ReceiveMessage(context).Returns(receivedMessage);

            decorator.ReceiveMessage(context);

            history.Received(1).RecordProcessingStart(Arg.Any<string>());
        }

        [TestMethod]
        public void ReceiveMessage_When_Disabled_Does_Not_Record()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: false);
            var context = CreateContext();
            var receivedMessage = Substitute.For<IReceivedMessageInternal>();
            inner.ReceiveMessage(context).Returns(receivedMessage);

            decorator.ReceiveMessage(context);

            history.DidNotReceive().RecordProcessingStart(Arg.Any<string>());
        }

        [TestMethod]
        public void ReceiveMessage_When_Null_Result_Does_Not_Record()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: true, trackProcessing: true);
            var context = CreateContext();
            inner.ReceiveMessage(context).Returns((IReceivedMessageInternal)null);

            decorator.ReceiveMessage(context);

            history.DidNotReceive().RecordProcessingStart(Arg.Any<string>());
        }

        [TestMethod]
        public void ReceiveMessage_When_History_Throws_Exception_Is_Swallowed()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: true, trackProcessing: true);
            var context = CreateContext();
            var receivedMessage = Substitute.For<IReceivedMessageInternal>();
            inner.ReceiveMessage(context).Returns(receivedMessage);
            history.When(h => h.RecordProcessingStart(Arg.Any<string>()))
                .Do(_ => throw new InvalidOperationException("history write failed"));

            var result = decorator.ReceiveMessage(context);

            Assert.AreSame(receivedMessage, result);
        }

        [TestMethod]
        public async Task ReceiveMessageAsync_Calls_Inner_Handler_And_Returns_Result()
        {
            var (decorator, inner, _, _, _) = CreateDecorator(enabled: false);
            var context = CreateContext();
            var expectedResult = Substitute.For<IReceivedMessageInternal>();
            SetupAsyncReceive(inner, context, expectedResult);

            var result = await decorator.ReceiveMessageAsync(context, CancellationToken.None);

            Assert.AreSame(expectedResult, result);
        }

        [TestMethod]
        public async Task ReceiveMessageAsync_When_Enabled_Records_Processing_Start()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: true, trackProcessing: true);
            var context = CreateContext();
            var receivedMessage = Substitute.For<IReceivedMessageInternal>();
            SetupAsyncReceive(inner, context, receivedMessage);

            await decorator.ReceiveMessageAsync(context, CancellationToken.None);

            history.Received(1).RecordProcessingStart(Arg.Any<string>());
        }

        [TestMethod]
        public async Task ReceiveMessageAsync_When_Disabled_Does_Not_Record()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: false);
            var context = CreateContext();
            var receivedMessage = Substitute.For<IReceivedMessageInternal>();
            SetupAsyncReceive(inner, context, receivedMessage);

            await decorator.ReceiveMessageAsync(context, CancellationToken.None);

            history.DidNotReceive().RecordProcessingStart(Arg.Any<string>());
        }

        [TestMethod]
        public async Task ReceiveMessageAsync_When_Null_Result_Does_Not_Record()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: true, trackProcessing: true);
            var context = CreateContext();
            SetupAsyncReceive(inner, context, null);

            await decorator.ReceiveMessageAsync(context, CancellationToken.None);

            history.DidNotReceive().RecordProcessingStart(Arg.Any<string>());
        }

        [TestMethod]
        public async Task ReceiveMessageAsync_When_History_Throws_Exception_Is_Swallowed()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: true, trackProcessing: true);
            var context = CreateContext();
            var receivedMessage = Substitute.For<IReceivedMessageInternal>();
            SetupAsyncReceive(inner, context, receivedMessage);
            history.When(h => h.RecordProcessingStart(Arg.Any<string>()))
                .Do(_ => throw new InvalidOperationException("history write failed"));

            var result = await decorator.ReceiveMessageAsync(context, CancellationToken.None);

            Assert.AreSame(receivedMessage, result);
        }

        [TestMethod]
        public void IsBlockingOperation_Delegates_To_Inner()
        {
            var (decorator, inner, _, _, _) = CreateDecorator(enabled: false);
            inner.IsBlockingOperation.Returns(true);

            Assert.IsTrue(decorator.IsBlockingOperation);

            inner.IsBlockingOperation.Returns(false);

            Assert.IsFalse(decorator.IsBlockingOperation);
        }

        private static void SetupAsyncReceive(IReceiveMessages inner, IMessageContext context,
            IReceivedMessageInternal result)
        {
            //Configuring a substitute is the one case where the returned ValueTask is not meant to be
            //consumed at all - NSubstitute intercepts the call to record the setup. Kept in this single
            //helper so the suppression does not spread across the tests.
#pragma warning disable CA2012
            inner.ReceiveMessageAsync(context, Arg.Any<CancellationToken>())
                .Returns(new ValueTask<IReceivedMessageInternal>(result));
#pragma warning restore CA2012
        }

        private static IMessageContext CreateContext()
        {
            var context = Substitute.For<IMessageContext>();
            var messageId = Substitute.For<IMessageId>();
            var setting = Substitute.For<ISetting>();
            setting.Value.Returns(42L);
            messageId.Id.Returns(setting);
            messageId.HasValue.Returns(true);
            context.MessageId.Returns(messageId);
            return context;
        }

        private static (IReceiveMessages decorator, IReceiveMessages inner, IWriteMessageHistory history,
            IBaseTransportOptions options, ILogger log) CreateDecorator(
            bool enabled = false, bool trackProcessing = true)
        {
            var inner = Substitute.For<IReceiveMessages>();
            var history = Substitute.For<IWriteMessageHistory>();
            var historyOptions = Substitute.For<IHistoryTransportOptions>();
            historyOptions.TrackProcessing.Returns(trackProcessing);
            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(enabled);
            options.HistoryOptions.Returns(historyOptions);
            ILogger log = NullLogger.Instance;

            var decorator = new ReceiveMessagesHistoryDecorator(inner, history, options, log);
            return (decorator, inner, history, options, log);
        }
    }
}
