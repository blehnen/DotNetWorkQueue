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
