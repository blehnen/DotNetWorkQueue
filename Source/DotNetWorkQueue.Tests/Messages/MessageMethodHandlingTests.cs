using System;
using System.Diagnostics.CodeAnalysis;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Queue;


using Xunit;

namespace DotNetWorkQueue.Tests.Messages
{
    public class MessageMethodHandlingTests
    {
        [Fact]
        public void IsDisposed_False_By_Default()
        {
            using (var test = Create())
            {
                Assert.False(test.IsDisposed);
            }
        }

        [Fact]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var test = Create();
            test.Dispose();
            Assert.True(test.IsDisposed);
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [Fact]
        public void Call_Dispose_Multiple_Times_Ok()
        {
            using (var test = Create())
            {
                test.Dispose();
            }
        }

        [Fact]
        public void Disposed_Instance_HandleExecution_Exception()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<IReceivedMessageInternal>();
            message.Body.Returns(new MessageExpression());
            var test = Create();
            test.Dispose();
            Assert.Throws<ObjectDisposedException>(
                delegate
                {
                    test.HandleExecution(new ReceivedMessage<MessageExpression>(message, new GetPreviousErrorsNoOp()), new WorkerNotificationNoOp());
                });
        }


        [Fact]
        public void Calling_HandleExecution_Null_Exception()
        {
            using (var test = Create())
            {
                Assert.Throws<ArgumentNullException>(
                    delegate
                    {
                        test.HandleExecution(null, null);
                    });
            }
        }

        [Fact]
        public void Calling_HandleExecution_Null_Exception2()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<IReceivedMessageInternal>();
            message.Body.Returns(new MessageExpression());

            using (var test = Create())
            {
                Assert.Throws<ArgumentNullException>(
                    delegate
                    {
                        test.HandleExecution(
                            new ReceivedMessage<MessageExpression>(message, new GetPreviousErrorsNoOp()), null);
                    });
            }
        }

        [Fact]
        public void Calling_HandleExecution_Null_Exception3()
        {
            using (var test = Create())
            {
                Assert.Throws<ArgumentNullException>(
                    delegate
                    {
                        test.HandleExecution(
                            null, new WorkerNotificationNoOp());
                    });
            }
        }

        private IMessageMethodHandling Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<MessageMethodHandling>();
        }
    }
}
