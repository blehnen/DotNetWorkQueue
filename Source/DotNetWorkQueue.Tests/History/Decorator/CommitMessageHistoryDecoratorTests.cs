using System;
using System.Threading.Tasks;
using DotNetWorkQueue.History.Decorator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.History.Decorator
{
    [TestClass]
    public class CommitMessageHistoryDecoratorTests
    {
        [TestMethod]
        public void Commit_Calls_Inner_Handler_And_Returns_Result()
        {
            var (decorator, inner, _, _, _) = CreateDecorator(enabled: false);
            var context = CreateContext();
            inner.Commit(context).Returns(true);

            var result = decorator.Commit(context);

            Assert.IsTrue(result);
            inner.Received(1).Commit(context);
        }

        [TestMethod]
        public void Commit_When_Enabled_And_TrackComplete_Records_History()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: true, trackComplete: true);
            var context = CreateContext();
            inner.Commit(context).Returns(true);

            var result = decorator.Commit(context);

            Assert.IsTrue(result);
            history.Received(1).RecordComplete(Arg.Any<string>());
        }

        [TestMethod]
        public void Commit_When_Disabled_Does_Not_Record_History()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: false);
            var context = CreateContext();
            inner.Commit(context).Returns(true);

            decorator.Commit(context);

            history.DidNotReceive().RecordComplete(Arg.Any<string>());
        }

        [TestMethod]
        public void Commit_When_Inner_Returns_False_Does_Not_Record_History()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: true, trackComplete: true);
            var context = CreateContext();
            inner.Commit(context).Returns(false);

            var result = decorator.Commit(context);

            Assert.IsFalse(result);
            history.DidNotReceive().RecordComplete(Arg.Any<string>());
        }

        [TestMethod]
        public void Commit_When_History_Throws_Exception_Is_Swallowed()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: true, trackComplete: true);
            var context = CreateContext();
            inner.Commit(context).Returns(true);
            history.When(h => h.RecordComplete(Arg.Any<string>()))
                .Do(_ => throw new InvalidOperationException("history write failed"));

            var result = decorator.Commit(context);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task CommitAsync_When_Enabled_Records_History_Without_Blocking()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: true, trackComplete: true);
            var context = CreateContext();
            inner.CommitAsync(context).Returns(Task.FromResult(true));

            var result = await decorator.CommitAsync(context).ConfigureAwait(false);

            Assert.IsTrue(result);
            await history.Received(1).RecordCompleteAsync(Arg.Any<string>()).ConfigureAwait(false);
            //Calling the blocking member here would satisfy every other assertion while holding a
            //thread-pool thread for the write.
            history.DidNotReceive().RecordComplete(Arg.Any<string>());
        }

        [TestMethod]
        public async Task CommitAsync_When_Inner_Returns_False_Does_Not_Record_History()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: true, trackComplete: true);
            var context = CreateContext();
            inner.CommitAsync(context).Returns(Task.FromResult(false));

            var result = await decorator.CommitAsync(context).ConfigureAwait(false);

            Assert.IsFalse(result);
            await history.DidNotReceive().RecordCompleteAsync(Arg.Any<string>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CommitAsync_When_History_Throws_The_Commit_Still_Succeeds()
        {
            var (decorator, inner, history, _, _) = CreateDecorator(enabled: true, trackComplete: true);
            var context = CreateContext();
            inner.CommitAsync(context).Returns(Task.FromResult(true));
            history.RecordCompleteAsync(Arg.Any<string>())
                .Returns(Task.FromException(new InvalidOperationException("history write failed")));

            //History is bookkeeping. Failing to write it must not turn a committed message into a
            //failed one, which would roll back work that has already been done.
            var result = await decorator.CommitAsync(context).ConfigureAwait(false);

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

        private static (ICommitMessage decorator, ICommitMessage inner, IWriteMessageHistory history,
            IBaseTransportOptions options, ILogger log) CreateDecorator(
            bool enabled = false, bool trackComplete = true)
        {
            var inner = Substitute.For<ICommitMessage>();
            var history = Substitute.For<IWriteMessageHistory>();
            var historyOptions = Substitute.For<IHistoryTransportOptions>();
            historyOptions.TrackComplete.Returns(trackComplete);
            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(enabled);
            options.HistoryOptions.Returns(historyOptions);
            ILogger log = NullLogger.Instance;

            var decorator = new CommitMessageHistoryDecorator(inner, history, options, log);
            return (decorator, inner, history, options, log);
        }
    }
}
