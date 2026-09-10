using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Notifications;
using DotNetWorkQueue.Queue;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class MessageExceptionHandlerTests
    {
        [TestMethod]
        public void Message_Handled()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var error = fixture.Create<IReceiveMessagesError>();
            fixture.Inject(error);
            var test = fixture.Create<MessageExceptionHandler>();

            var message = fixture.Create<IReceivedMessageInternal>();
            var context = fixture.Create<IMessageContext>();
            var exception = new Exception();

            Assert.ThrowsExactly<MessageException>(
           delegate
           {
               test.Handle(message, context, exception);
           });

            error.Received(1).MessageFailedProcessing(message, context, exception);
        }
        [TestMethod]
        public void Message_Handled_Exception_Throws_Exception()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            IReceiveMessagesError error = fixture.Create<ReceiveMessagesErrorWillCrash>();
            IConsumerQueueErrorNotification notify = fixture.Create<IConsumerQueueErrorNotification>();
            fixture.Inject(error);
            var test = new MessageExceptionHandler(error, Substitute.For<ILogger>(), notify);

            var message = fixture.Create<IReceivedMessageInternal>();
            var context = fixture.Create<IMessageContext>();
            var exception = new Exception();

            Assert.ThrowsExactly<DotNetWorkQueueException>(
            delegate
            {
                test.Handle(message, context, exception);
            });
        }
        [TestMethod]
        public async Task Message_Handled_Async()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var error = fixture.Create<IReceiveMessagesError>();
            fixture.Inject(error);
            var test = fixture.Create<MessageExceptionHandler>();

            var message = fixture.Create<IReceivedMessageInternal>();
            var context = fixture.Create<IMessageContext>();
            var exception = new Exception();

            await Assert.ThrowsExactlyAsync<MessageException>(
                () => test.HandleAsync(message, context, exception));

            await error.Received(1).MessageFailedProcessingAsync(message, context, exception);
            error.DidNotReceive().MessageFailedProcessing(message, context, exception);
        }

        [TestMethod]
        public async Task Message_Handled_Async_Exception_Throws_Exception()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            IReceiveMessagesError error = fixture.Create<ReceiveMessagesErrorWillCrash>();
            IConsumerQueueErrorNotification notify = fixture.Create<IConsumerQueueErrorNotification>();
            fixture.Inject(error);
            var test = new MessageExceptionHandler(error, Substitute.For<ILogger>(), notify);

            var message = fixture.Create<IReceivedMessageInternal>();
            var context = fixture.Create<IMessageContext>();
            var exception = new Exception();

            await Assert.ThrowsExactlyAsync<DotNetWorkQueueException>(
                () => test.HandleAsync(message, context, exception));
        }

        [TestMethod]
        public async Task Message_Handled_Async_Moved_To_Error_Queue_Does_Not_Throw()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var error = fixture.Create<IReceiveMessagesError>();
            var notify = fixture.Create<IConsumerQueueErrorNotification>();
            var message = fixture.Create<IReceivedMessageInternal>();
            var context = fixture.Create<IMessageContext>();
            var exception = new Exception();
            error.MessageFailedProcessingAsync(message, context, exception)
                .Returns(ReceiveMessagesErrorResult.Error);
            var test = new MessageExceptionHandler(error, Substitute.For<ILogger>(), notify);

            //the message is gone, so the failure is not re-thrown - it is reported instead
            await test.HandleAsync(message, context, exception);

            notify.Received(1).InvokeMovedToErrorQueue(Arg.Any<ErrorNotification>());
        }

        [TestMethod]
        public void Message_Handled_Moved_To_Error_Queue_Notification_Keeps_MessageId()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var error = fixture.Create<IReceiveMessagesError>();
            var notify = fixture.Create<IConsumerQueueErrorNotification>();
            var message = fixture.Create<IReceivedMessageInternal>();
            var context = fixture.Create<IMessageContext>();
            var messageId = fixture.Create<IMessageId>();
            context.MessageId.Returns(messageId);
            var exception = new Exception();
            //every built-in transport clears the id from the context once the message is in the error
            //queue, which is why the notification cannot read it back off the context afterwards
            error.MessageFailedProcessing(message, context, exception).Returns(_ =>
            {
                context.MessageId.Returns((IMessageId)null);
                return ReceiveMessagesErrorResult.Error;
            });
            var test = new MessageExceptionHandler(error, Substitute.For<ILogger>(), notify);

            test.Handle(message, context, exception);

            notify.Received(1).InvokeMovedToErrorQueue(Arg.Is<ErrorNotification>(n => n.MessageId == messageId));
        }

        [TestMethod]
        public async Task Message_Handled_Async_Moved_To_Error_Queue_Notification_Keeps_MessageId()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var error = fixture.Create<IReceiveMessagesError>();
            var notify = fixture.Create<IConsumerQueueErrorNotification>();
            var message = fixture.Create<IReceivedMessageInternal>();
            var context = fixture.Create<IMessageContext>();
            var messageId = fixture.Create<IMessageId>();
            context.MessageId.Returns(messageId);
            var exception = new Exception();
            error.MessageFailedProcessingAsync(message, context, exception).Returns(_ =>
            {
                context.MessageId.Returns((IMessageId)null);
                return Task.FromResult(ReceiveMessagesErrorResult.Error);
            });
            var test = new MessageExceptionHandler(error, Substitute.For<ILogger>(), notify);

            await test.HandleAsync(message, context, exception);

            notify.Received(1).InvokeMovedToErrorQueue(Arg.Is<ErrorNotification>(n => n.MessageId == messageId));
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class ReceiveMessagesErrorWillCrash : IReceiveMessagesError
        {
            public ReceiveMessagesErrorResult MessageFailedProcessing(IReceivedMessageInternal message, IMessageContext context, Exception exception)
            {
                throw new NotImplementedException();
            }

            public Task<ReceiveMessagesErrorResult> MessageFailedProcessingAsync(IReceivedMessageInternal message, IMessageContext context, Exception exception)
            {
                throw new NotImplementedException();
            }
        }
    }
}
