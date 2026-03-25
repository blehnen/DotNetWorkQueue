using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetWorkQueue.History.Decorator;
using DotNetWorkQueue.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests.History.Decorator
{
    [TestClass]
    public class SendMessagesHistoryDecoratorTests
    {
        [TestMethod]
        public void Send_Calls_Inner_Handler_And_Returns_Result()
        {
            var (decorator, inner, _, _, _) = CreateDecorator(enabled: false);
            var message = Substitute.For<IMessage>();
            var data = Substitute.For<IAdditionalMessageData>();
            var expectedResult = CreateOutputMessage(hasError: true);
            inner.Send(message, data).Returns(expectedResult);

            var result = decorator.Send(message, data);

            Assert.AreSame(expectedResult, result);
            inner.Received(1).Send(message, data);
        }

        [TestMethod]
        public void Send_When_Enabled_And_TrackEnqueue_Records_History()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: true, trackEnqueue: true);
            var message = Substitute.For<IMessage>();
            var data = Substitute.For<IAdditionalMessageData>();
            data.Route.Returns("test-route");
            var outputMessage = CreateOutputMessage(hasError: false);
            inner.Send(message, data).Returns(outputMessage);

            decorator.Send(message, data);

            history.Received(1).RecordEnqueue(
                Arg.Any<string>(),
                Arg.Any<string>(),
                "test-route",
                Arg.Any<string>(),
                Arg.Any<byte[]>(),
                Arg.Any<byte[]>());
        }

        [TestMethod]
        public void Send_When_Disabled_Does_Not_Record_History()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: false);
            var message = Substitute.For<IMessage>();
            var data = Substitute.For<IAdditionalMessageData>();
            var outputMessage = CreateOutputMessage(hasError: false);
            inner.Send(message, data).Returns(outputMessage);

            decorator.Send(message, data);

            history.DidNotReceive().RecordEnqueue(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<byte[]>(),
                Arg.Any<byte[]>());
        }

        [TestMethod]
        public void Send_When_HasError_Does_Not_Record_History()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: true, trackEnqueue: true);
            var message = Substitute.For<IMessage>();
            var data = Substitute.For<IAdditionalMessageData>();
            var outputMessage = CreateOutputMessage(hasError: true);
            inner.Send(message, data).Returns(outputMessage);

            decorator.Send(message, data);

            history.DidNotReceive().RecordEnqueue(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<byte[]>(),
                Arg.Any<byte[]>());
        }

        [TestMethod]
        public void Send_When_History_Throws_Exception_Is_Swallowed()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: true, trackEnqueue: true);
            var message = Substitute.For<IMessage>();
            var data = Substitute.For<IAdditionalMessageData>();
            var outputMessage = CreateOutputMessage(hasError: false);
            inner.Send(message, data).Returns(outputMessage);
            history.When(h => h.RecordEnqueue(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<byte[]>(),
                    Arg.Any<byte[]>()))
                .Do(_ => throw new InvalidOperationException("history write failed"));

            var result = decorator.Send(message, data);

            Assert.AreSame(outputMessage, result);
        }

        [TestMethod]
        public async Task SendAsync_Calls_Inner_Handler_And_Returns_Result()
        {
            var (decorator, inner, _, _, _) = CreateDecorator(enabled: false);
            var message = Substitute.For<IMessage>();
            var data = Substitute.For<IAdditionalMessageData>();
            var expectedResult = CreateOutputMessage(hasError: true);
            inner.SendAsync(message, data).Returns(Task.FromResult(expectedResult));

            var result = await decorator.SendAsync(message, data);

            Assert.AreSame(expectedResult, result);
            await inner.Received(1).SendAsync(message, data);
        }

        [TestMethod]
        public async Task SendAsync_When_Enabled_Records_History()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: true, trackEnqueue: true);
            var message = Substitute.For<IMessage>();
            var data = Substitute.For<IAdditionalMessageData>();
            var outputMessage = CreateOutputMessage(hasError: false);
            inner.SendAsync(message, data).Returns(Task.FromResult(outputMessage));

            await decorator.SendAsync(message, data);

            history.Received(1).RecordEnqueue(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<byte[]>(),
                Arg.Any<byte[]>());
        }

        [TestMethod]
        public void BatchSend_When_Enabled_Records_For_Each_Message()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: true, trackEnqueue: true);
            var messages = new List<QueueMessage<IMessage, IAdditionalMessageData>>();

            var msg1 = CreateOutputMessage(hasError: false);
            var msg2 = CreateOutputMessage(hasError: false);
            var batchResult = Substitute.For<IQueueOutputMessages>();
            batchResult.GetEnumerator().Returns(_ => new List<IQueueOutputMessage> { msg1, msg2 }.GetEnumerator());
            inner.Send(messages).Returns(batchResult);

            decorator.Send(messages);

            history.Received(2).RecordEnqueue(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<byte[]>(),
                Arg.Any<byte[]>());
        }

        [TestMethod]
        public async Task BatchSendAsync_When_Enabled_Records_For_Each_Message()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: true, trackEnqueue: true);
            var messages = new List<QueueMessage<IMessage, IAdditionalMessageData>>();

            var msg1 = CreateOutputMessage(hasError: false);
            var msg2 = CreateOutputMessage(hasError: false);
            var msg3 = CreateOutputMessage(hasError: false);
            var batchResult = Substitute.For<IQueueOutputMessages>();
            batchResult.GetEnumerator().Returns(_ => new List<IQueueOutputMessage> { msg1, msg2, msg3 }.GetEnumerator());
            inner.SendAsync(messages).Returns(Task.FromResult(batchResult));

            await decorator.SendAsync(messages);

            history.Received(3).RecordEnqueue(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<byte[]>(),
                Arg.Any<byte[]>());
        }

        private static IQueueOutputMessage CreateOutputMessage(bool hasError)
        {
            var result = Substitute.For<IQueueOutputMessage>();
            result.HasError.Returns(hasError);
            if (!hasError)
            {
                var sentMessage = Substitute.For<ISentMessage>();
                var messageId = Substitute.For<IMessageId>();
                var setting = Substitute.For<ISetting>();
                setting.Value.Returns(42L);
                messageId.Id.Returns(setting);
                messageId.HasValue.Returns(true);
                sentMessage.MessageId.Returns(messageId);

                var correlationId = Substitute.For<ICorrelationId>();
                var corrSetting = Substitute.For<ISetting>();
                corrSetting.Value.Returns(99L);
                correlationId.Id.Returns(corrSetting);
                sentMessage.CorrelationId.Returns(correlationId);

                result.SentMessage.Returns(sentMessage);
            }
            return result;
        }

        private static (ISendMessages decorator, ISendMessages inner, IWriteMessageHistory history,
            IBaseTransportOptions options, ILogger log) CreateDecorator(
            bool enabled = false, bool trackEnqueue = true)
        {
            var inner = Substitute.For<ISendMessages>();
            var history = Substitute.For<IWriteMessageHistory>();
            var historyOptions = Substitute.For<IHistoryTransportOptions>();
            historyOptions.TrackEnqueue.Returns(trackEnqueue);
            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(enabled);
            options.HistoryOptions.Returns(historyOptions);
            ILogger log = NullLogger.Instance;

            var decorator = new SendMessagesHistoryDecorator(inner, history, options, log);
            return (decorator, inner, history, options, log);
        }
    }
}
