using System;
using DotNetWorkQueue.History.Decorator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests.History.Decorator
{
    [TestClass]
    public class RollbackMessageHistoryDecoratorTests
    {
        [TestMethod]
        public void Rollback_Calls_Inner_Handler_And_Returns_Result()
        {
            var (decorator, inner, _, _, _) = CreateDecorator(enabled: false);
            var context = CreateContext();
            inner.Rollback(context).Returns(true);

            var result = decorator.Rollback(context);

            Assert.IsTrue(result);
            inner.Received(1).Rollback(context);
        }

        [TestMethod]
        public void Rollback_When_Enabled_Records_History()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: true);
            var context = CreateContext();
            inner.Rollback(context).Returns(true);

            decorator.Rollback(context);

            history.Received(1).RecordRollback(Arg.Any<string>());
        }

        [TestMethod]
        public void Rollback_When_Disabled_Does_Not_Record()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: false);
            var context = CreateContext();
            inner.Rollback(context).Returns(true);

            decorator.Rollback(context);

            history.DidNotReceive().RecordRollback(Arg.Any<string>());
        }

        [TestMethod]
        public void Rollback_When_History_Throws_Exception_Is_Swallowed()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: true);
            var context = CreateContext();
            inner.Rollback(context).Returns(true);
            history.When(h => h.RecordRollback(Arg.Any<string>()))
                .Do(_ => throw new InvalidOperationException("history write failed"));

            var result = decorator.Rollback(context);

            Assert.IsTrue(result);
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

        private static (IRollbackMessage decorator, IRollbackMessage inner, IWriteMessageHistory history,
            IBaseTransportOptions options, ILogger log) CreateDecorator(
            bool enabled = false)
        {
            var inner = Substitute.For<IRollbackMessage>();
            var history = Substitute.For<IWriteMessageHistory>();
            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(enabled);
            ILogger log = NullLogger.Instance;

            var decorator = new RollbackMessageHistoryDecorator(inner, history, options, log);
            return (decorator, inner, history, options, log);
        }
    }
}
