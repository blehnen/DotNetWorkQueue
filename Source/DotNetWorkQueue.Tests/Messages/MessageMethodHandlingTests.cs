using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Queue;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

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

        [TestMethod]
        [DataRow(MessageExpressionPayloads.Action)]
        [DataRow(MessageExpressionPayloads.ActionRaw)]
        public void HandleExecution_Throws_The_Methods_Own_Exception_With_Its_Stack_Trace(MessageExpressionPayloads payload)
        {
            var error = Assert.ThrowsExactly<InvalidDataException>(() => RunThrowingMethod(payload));

            // The frame that threw must survive; "Throw" alone would also match Assert.ThrowsExactly's own frames.
            Assert.Contains($"{nameof(ThrowingTarget)}.{nameof(ThrowingTarget.ThrowFromQueuedMethod)}(", error.StackTrace);
        }

        [TestMethod]
        [DataRow(MessageExpressionPayloads.Action)]
        [DataRow(MessageExpressionPayloads.ActionRaw)]
        public void HandleExecution_Does_Not_Wrap_The_Methods_Exception(MessageExpressionPayloads payload)
        {
            // A TargetInvocationException's message is localized, so code that unwraps one by its text
            // only works on an English runtime. The method is invoked directly, so none should be raised.
            var wrapped = 0;
            void OnFirstChance(object sender, FirstChanceExceptionEventArgs e)
            {
                if (e.Exception is TargetInvocationException { InnerException: InvalidDataException inner }
                    && inner.Message == ThrowingTarget.Message)
                    Interlocked.Increment(ref wrapped);
            }

            AppDomain.CurrentDomain.FirstChanceException += OnFirstChance;
            try
            {
                Assert.ThrowsExactly<InvalidDataException>(() => RunThrowingMethod(payload));
            }
            finally
            {
                AppDomain.CurrentDomain.FirstChanceException -= OnFirstChance;
            }

            Assert.AreEqual(0, wrapped);
        }

        private static void RunThrowingMethod(MessageExpressionPayloads payload)
        {
            Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> method =
                (message, notification) => ThrowingTarget.ThrowFromQueuedMethod();

            var serializer = Substitute.For<IExpressionSerializer>();
            serializer.ConvertBytesToMethod(Arg.Any<byte[]>()).Returns(method);

            var body = payload == MessageExpressionPayloads.Action
                ? new MessageExpression(payload, new byte[] { 1 })
                : new MessageExpression(payload, method);

            using (var test = new MessageMethodHandling(serializer))
            {
                test.HandleExecution(CreateMessage(body), new WorkerNotificationNoOp());
            }
        }

        private static IReceivedMessage<MessageExpression> CreateMessage(MessageExpression body)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<IReceivedMessageInternal>();
            // Body is dynamic, so call Returns statically; a dynamic call cannot bind an extension method.
            SubstituteExtensions.Returns<object>(message.Body, body);
            return new ReceivedMessage<MessageExpression>(message, new GetPreviousErrorsNoOp(), NullLoggerFactory.Instance.CreateLogger("null"));
        }

        private IMessageMethodHandling Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<MessageMethodHandling>();
        }

        public static class ThrowingTarget
        {
            public const string Message = "thrown by the queued method";

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowFromQueuedMethod()
            {
                throw new InvalidDataException(Message);
            }
        }
    }
}
