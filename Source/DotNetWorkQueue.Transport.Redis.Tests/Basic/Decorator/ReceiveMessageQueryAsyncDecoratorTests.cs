using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Decorator
{
    /// <summary>
    /// The metrics and logging decorators on the asynchronous receive query.
    ///
    /// These exist because the async path needs its own decorators: the assembly scan that registers
    /// the synchronous query handler does not cover the asynchronous interface, so without explicit
    /// registrations an expired message silently stops being counted and logged the moment a consumer
    /// switches to the async receive. That is a decorator present on only one code path, which is the
    /// kind of difference nothing else would catch.
    /// </summary>
    [TestClass]
    public class ReceiveMessageQueryAsyncDecoratorTests
    {
        [TestMethod]
        public async Task Metrics_CountsAnExpiredMessage()
        {
            var (handler, query) = Handler(expired: true);
            var metrics = Substitute.For<IMetrics>();
            var counter = Substitute.For<ICounter>();
            metrics.Counter(Arg.Any<string>(), Arg.Any<Units>()).Returns(counter);

            var decorator = new Redis.Basic.Metrics.Decorator.ReceiveMessageQueryAsyncDecorator(
                metrics, handler, Substitute.For<IConnectionInformation>());

            await decorator.HandleAsync(query);

            counter.Received(1).Increment(1);
        }

        [TestMethod]
        public async Task Metrics_DoesNotCountAMessageThatDidNotExpire()
        {
            var (handler, query) = Handler(expired: false);
            var metrics = Substitute.For<IMetrics>();
            var counter = Substitute.For<ICounter>();
            metrics.Counter(Arg.Any<string>(), Arg.Any<Units>()).Returns(counter);

            var decorator = new Redis.Basic.Metrics.Decorator.ReceiveMessageQueryAsyncDecorator(
                metrics, handler, Substitute.For<IConnectionInformation>());

            await decorator.HandleAsync(query);

            counter.DidNotReceive().Increment(Arg.Any<long>());
        }

        [TestMethod]
        public async Task Metrics_HandlesAnEmptyQueue()
        {
            var handler = Substitute.For<IQueryHandlerAsync<ReceiveMessageQuery, RedisMessage>>();
            var query = new ReceiveMessageQuery(Substitute.For<IMessageContext>());
            handler.HandleAsync(query).Returns(Task.FromResult<RedisMessage>(null));

            var metrics = Substitute.For<IMetrics>();
            var counter = Substitute.For<ICounter>();
            metrics.Counter(Arg.Any<string>(), Arg.Any<Units>()).Returns(counter);

            var decorator = new Redis.Basic.Metrics.Decorator.ReceiveMessageQueryAsyncDecorator(
                metrics, handler, Substitute.For<IConnectionInformation>());

            Assert.IsNull(await decorator.HandleAsync(query));
            counter.DidNotReceive().Increment(Arg.Any<long>());
        }

        [TestMethod]
        public async Task Logging_ReturnsTheMessageForBothOutcomes()
        {
            foreach (var expired in new[] { true, false })
            {
                var (handler, query) = Handler(expired);
                var decorator = new Redis.Basic.Logging.Decorator.ReceiveMessageQueryAsyncDecorator(
                    NullLogger.Instance, handler);

                var result = await decorator.HandleAsync(query);

                Assert.IsNotNull(result);
                Assert.AreEqual(expired, result.Expired);
            }
        }

        [TestMethod]
        public async Task Logging_HandlesAnEmptyQueue()
        {
            var handler = Substitute.For<IQueryHandlerAsync<ReceiveMessageQuery, RedisMessage>>();
            var query = new ReceiveMessageQuery(Substitute.For<IMessageContext>());
            handler.HandleAsync(query).Returns(Task.FromResult<RedisMessage>(null));

            var decorator = new Redis.Basic.Logging.Decorator.ReceiveMessageQueryAsyncDecorator(
                NullLogger.Instance, handler);

            Assert.IsNull(await decorator.HandleAsync(query));
        }

        private static (IQueryHandlerAsync<ReceiveMessageQuery, RedisMessage> handler, ReceiveMessageQuery query)
            Handler(bool expired)
        {
            var handler = Substitute.For<IQueryHandlerAsync<ReceiveMessageQuery, RedisMessage>>();
            var query = new ReceiveMessageQuery(Substitute.For<IMessageContext>());
            var message = expired
                ? new RedisMessage("1", null, true)
                : new RedisMessage("1", Substitute.For<IReceivedMessageInternal>(), false);
            handler.HandleAsync(query).Returns(Task.FromResult(message));
            return (handler, query);
        }
    }
}
