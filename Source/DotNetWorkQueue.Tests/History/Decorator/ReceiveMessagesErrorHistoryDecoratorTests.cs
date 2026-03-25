using System;
using DotNetWorkQueue.History.Decorator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests.History.Decorator
{
    [TestClass]
    public class ReceiveMessagesErrorHistoryDecoratorTests
    {
        [TestMethod]
        public void MessageFailedProcessing_Calls_Inner_Handler_And_Returns_Result()
        {
            var (decorator, inner, _, _, _) = CreateDecorator(enabled: false);
            var context = CreateContext();
            var message = Substitute.For<IReceivedMessageInternal>();
            var exception = new InvalidOperationException("test error");
            inner.MessageFailedProcessing(message, context, exception).Returns(ReceiveMessagesErrorResult.Error);

            var result = decorator.MessageFailedProcessing(message, context, exception);

            Assert.AreEqual(ReceiveMessagesErrorResult.Error, result);
            inner.Received(1).MessageFailedProcessing(message, context, exception);
        }

        [TestMethod]
        public void MessageFailedProcessing_When_Enabled_Records_Error()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: true, trackError: true);
            var context = CreateContext();
            var message = Substitute.For<IReceivedMessageInternal>();
            var exception = new InvalidOperationException("test error");
            inner.MessageFailedProcessing(message, context, exception).Returns(ReceiveMessagesErrorResult.Error);

            decorator.MessageFailedProcessing(message, context, exception);

            history.Received(1).RecordError(Arg.Any<string>(), Arg.Is<string>(s => s.Contains("test error")));
        }

        [TestMethod]
        public void MessageFailedProcessing_When_Disabled_Does_Not_Record()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: false);
            var context = CreateContext();
            var message = Substitute.For<IReceivedMessageInternal>();
            var exception = new InvalidOperationException("test error");
            inner.MessageFailedProcessing(message, context, exception).Returns(ReceiveMessagesErrorResult.Error);

            decorator.MessageFailedProcessing(message, context, exception);

            history.DidNotReceive().RecordError(Arg.Any<string>(), Arg.Any<string>());
        }

        [TestMethod]
        public void MessageFailedProcessing_Truncates_Long_Exception_Text()
        {
            var (decorator, inner, history, config, _) = CreateDecorator(enabled: true, trackError: true, maxExceptionLength: 50);
            var context = CreateContext();
            var message = Substitute.For<IReceivedMessageInternal>();
            var longMessage = new string('x', 200);
            var exception = new InvalidOperationException(longMessage);
            inner.MessageFailedProcessing(message, context, exception).Returns(ReceiveMessagesErrorResult.Error);

            decorator.MessageFailedProcessing(message, context, exception);

            history.Received(1).RecordError(
                Arg.Any<string>(),
                Arg.Is<string>(s => s.Length == 50));
        }

        [TestMethod]
        public void MessageFailedProcessing_When_History_Throws_Exception_Is_Swallowed()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: true, trackError: true);
            var context = CreateContext();
            var message = Substitute.For<IReceivedMessageInternal>();
            var exception = new InvalidOperationException("test error");
            inner.MessageFailedProcessing(message, context, exception).Returns(ReceiveMessagesErrorResult.Retry);
            history.When(h => h.RecordError(Arg.Any<string>(), Arg.Any<string>()))
                .Do(_ => throw new InvalidOperationException("history write failed"));

            var result = decorator.MessageFailedProcessing(message, context, exception);

            Assert.AreEqual(ReceiveMessagesErrorResult.Retry, result);
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

        private static (IReceiveMessagesError decorator, IReceiveMessagesError inner, IWriteMessageHistory history,
            IBaseTransportOptions options, ILogger log) CreateDecorator(
            bool enabled = false, bool trackError = true, int maxExceptionLength = 4000)
        {
            var inner = Substitute.For<IReceiveMessagesError>();
            var history = Substitute.For<IWriteMessageHistory>();
            var historyOptions = Substitute.For<IHistoryTransportOptions>();
            historyOptions.TrackError.Returns(trackError);
            historyOptions.MaxExceptionLength.Returns(maxExceptionLength);
            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(enabled);
            options.HistoryOptions.Returns(historyOptions);
            ILogger log = NullLogger.Instance;

            var decorator = new ReceiveMessagesErrorHistoryDecorator(inner, history, options, log);
            return (decorator, inner, history, options, log);
        }
    }
}
