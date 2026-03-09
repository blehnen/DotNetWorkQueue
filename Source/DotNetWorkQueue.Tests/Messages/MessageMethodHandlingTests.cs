using System;
using System.Diagnostics.CodeAnalysis;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Queue;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Messages
{
    [TestClass]
    public class MessageMethodHandlingTests
    {
        [TestMethod]
        public void IsDisposed_False_By_Default()
        {
            using (var test = Create())
            {
                Assert.IsFalse(test.IsDisposed);
            }
        }

        [TestMethod]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var test = Create();
            test.Dispose();
            Assert.IsTrue(test.IsDisposed);
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [TestMethod]
        public void Call_Dispose_Multiple_Times_Ok()
        {
            using (var test = Create())
            {
                test.Dispose();
            }
        }

        [TestMethod]
        public void Disposed_Instance_HandleExecution_Exception()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<IReceivedMessageInternal>();
            message.Body.Returns(new MessageExpression());
            var test = Create();
            test.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(
                delegate
                {
                    test.HandleExecution(new ReceivedMessage<MessageExpression>(message, new GetPreviousErrorsNoOp(), NullLoggerFactory.Instance.CreateLogger("null")), new WorkerNotificationNoOp());
                });
        }


        [TestMethod]
        public void Calling_HandleExecution_Null_Exception()
        {
            using (var test = Create())
            {
                Assert.ThrowsExactly<ArgumentNullException>(
                    delegate
                    {
                        test.HandleExecution(null, null);
                    });
            }
        }

        [TestMethod]
        public void Calling_HandleExecution_Null_Exception2()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<IReceivedMessageInternal>();
            message.Body.Returns(new MessageExpression());

            using (var test = Create())
            {
                Assert.ThrowsExactly<ArgumentNullException>(
                    delegate
                    {
                        test.HandleExecution(
                            new ReceivedMessage<MessageExpression>(message, new GetPreviousErrorsNoOp(), NullLoggerFactory.Instance.CreateLogger("null")), null);
                    });
            }
        }

        [TestMethod]
        public void Calling_HandleExecution_Null_Exception3()
        {
            using (var test = Create())
            {
                Assert.ThrowsExactly<ArgumentNullException>(
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
