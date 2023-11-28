using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Queue;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class MessageExceptionHandlerTests
    {
        [Fact]
        public void Message_Handled()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var error = fixture.Create<IReceiveMessagesError>();
            fixture.Inject(error);
            var test = fixture.Create<MessageExceptionHandler>();

            var message = fixture.Create<IReceivedMessageInternal>();
            var context = fixture.Create<IMessageContext>();
            var exception = new Exception();

            Assert.Throws<MessageException>(
           delegate
           {
               test.Handle(message, context, exception);
           });

            error.Received(1).MessageFailedProcessing(message, context, exception);
        }
        [Fact]
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

            Assert.Throws<DotNetWorkQueueException>(
            delegate
            {
                test.Handle(message, context, exception);
            });
        }
        // ReSharper disable once ClassNeverInstantiated.Local
        private class ReceiveMessagesErrorWillCrash : IReceiveMessagesError
        {
            public ReceiveMessagesErrorResult MessageFailedProcessing(IReceivedMessageInternal message, IMessageContext context, Exception exception)
            {
                throw new NotImplementedException();
            }
        }
    }
}
