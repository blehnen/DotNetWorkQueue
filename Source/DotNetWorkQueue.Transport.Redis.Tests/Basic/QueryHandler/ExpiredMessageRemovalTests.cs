// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using DotNetWorkQueue.Transport.Redis.Basic.Query;
using DotNetWorkQueue.Transport.Redis.Basic.QueryHandler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.QueryHandler
{
    /// <summary>
    /// Removing an expired message - the one thing the two receive handlers cannot share.
    ///
    /// The parse lives in <c>AReceiveMessageQueryHandler.BuildMessage</c> and is shared deliberately, so
    /// the expiry branch now reports the decision and each handler carries it out in its own idiom. The
    /// asynchronous one runs on the async receive path, where a blocking removal occupies a thread-pool
    /// thread, so it must call the awaitable member - and calling the blocking one instead would pass
    /// every functional assertion, which is why the negative is asserted too.
    /// </summary>
    [TestClass]
    public class ExpiredMessageRemovalTests
    {
        [TestMethod]
        public async Task Async_ExpiredMessage_RemovesWithoutBlocking()
        {
            var harness = new Harness();

            var result = await harness.CreateAsync().HandleAsync(harness.Query).ConfigureAwait(false);

            Assert.IsTrue(result.Expired, "The message should have been reported as expired.");
            await harness.RemoveMessage.Received(1)
                .RemoveAsync(harness.Context, RemoveMessageReason.Expired).ConfigureAwait(false);
            harness.RemoveMessage.DidNotReceiveWithAnyArgs()
                .Remove(Arg.Any<IMessageContext>(), Arg.Any<RemoveMessageReason>());
        }

        [TestMethod]
        public void Sync_ExpiredMessage_StillRemovesSynchronously()
        {
            var harness = new Harness();

            var result = harness.CreateSync().Handle(harness.Query);

            Assert.IsTrue(result.Expired, "The message should have been reported as expired.");
            harness.RemoveMessage.Received(1).Remove(harness.Context, RemoveMessageReason.Expired);
            harness.RemoveMessage.DidNotReceiveWithAnyArgs()
                .RemoveAsync(Arg.Any<IMessageContext>(), Arg.Any<RemoveMessageReason>());
        }

        [TestMethod]
        public async Task Async_WhenTheRemovalFails_SurfacesAsReceiveMessageException()
        {
            var harness = new Harness();
            harness.RemoveMessage
                .RemoveAsync(Arg.Any<IMessageContext>(), Arg.Any<RemoveMessageReason>())
                .Returns<Task<RemoveMessageStatus>>(_ => throw new InvalidOperationException("boom"));

            //The removal used to sit inside BuildMessage's try/catch. Moving it out must not change
            //what a caller sees when it fails.
            var thrown = await Assert.ThrowsExactlyAsync<Exceptions.ReceiveMessageException>(
                () => harness.CreateAsync().HandleAsync(harness.Query)).ConfigureAwait(false);

            Assert.IsInstanceOfType<InvalidOperationException>(thrown.InnerException);
        }

        private sealed class Harness
        {
            private const string MessageId = "a-message";
            private readonly ICompositeSerialization _serializer;
            private readonly IReceivedMessageFactory _receivedMessageFactory;
            private readonly RedisHeaders _redisHeaders;
            private readonly TestableDequeueLua _dequeueLua;
            private readonly IUnixTimeFactory _unixTimeFactory;
            private readonly IMessageFactory _messageFactory;

            public IRemoveMessage RemoveMessage { get; }
            public IMessageContext Context { get; }
            public ReceiveMessageQuery Query { get; }

            public Harness()
            {
                var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

                RemoveMessage = Substitute.For<IRemoveMessage>();
                Context = Substitute.For<IMessageContext>();
                Query = new ReceiveMessageQuery(Context);

                _receivedMessageFactory = Substitute.For<IReceivedMessageFactory>();
                _messageFactory = Substitute.For<IMessageFactory>();

                var unixTime = Substitute.For<IUnixTime>();
                unixTime.GetCurrentUnixTimestampMilliseconds().Returns(1000L);
                _unixTimeFactory = Substitute.For<IUnixTimeFactory>();
                _unixTimeFactory.Create().Returns(unixTime);

                var correlationHeader = Substitute.For<IMessageContextData<RedisQueueCorrelationIdSerialized>>();
                correlationHeader.Name.Returns("CorrelationId");
                var dataFactory = Substitute.For<IMessageContextDataFactory>();
                dataFactory.Create<RedisQueueCorrelationIdSerialized>(Arg.Any<string>(), Arg.Any<RedisQueueCorrelationIdSerialized>())
                    .Returns(correlationHeader);
                _redisHeaders = new RedisHeaders(dataFactory, Substitute.For<IHeaders>());

                //The expiry branch reads the correlation id out of the deserialized headers before it
                //removes, so they have to carry one for either path to reach the removal at all.
                var headers = new Dictionary<string, object>
                {
                    { "CorrelationId", new RedisQueueCorrelationIdSerialized(Guid.NewGuid()) }
                };
                _serializer = Substitute.For<ICompositeSerialization>();
                _serializer.InternalSerializer
                    .ConvertBytesTo<IDictionary<string, object>>(Arg.Any<byte[]>())
                    .Returns(headers);

                var connectionInformation = Substitute.For<IConnectionInformation>();
                connectionInformation.QueueName.Returns("testQueue");

                var configuration = fixture.Create<QueueConsumerConfiguration>();
                configuration.Routes.Clear();

                //result[3] is the expiration; below the current time is what makes it expired.
                _dequeueLua = new TestableDequeueLua(Substitute.For<IRedisConnection>(),
                    new RedisNames(connectionInformation), configuration)
                {
                    NextResult = RedisResult.Create(new RedisValue[]
                        { MessageId, new byte[] { 1 }, new byte[] { 2 }, 500L })
                };
            }

            public ReceiveMessageQueryHandler CreateSync() =>
                new ReceiveMessageQueryHandler(_serializer, _receivedMessageFactory, RemoveMessage,
                    _redisHeaders, _dequeueLua, _unixTimeFactory, _messageFactory);

            public ReceiveMessageQueryHandlerAsync CreateAsync() =>
                new ReceiveMessageQueryHandlerAsync(_serializer, _receivedMessageFactory, RemoveMessage,
                    _redisHeaders, _dequeueLua, _unixTimeFactory, _messageFactory);
        }

        private class TestableDequeueLua : DequeueLua
        {
            public TestableDequeueLua(IRedisConnection connection, RedisNames redisNames,
                QueueConsumerConfiguration configuration)
                : base(connection, redisNames, configuration)
            {
            }

            public RedisResult NextResult { get; set; } = RedisResult.Create(RedisValue.Null);

            public override RedisResult TryExecute(object parameters) => NextResult;

            public override Task<RedisResult> TryExecuteAsync(object parameters) => Task.FromResult(NextResult);
        }
    }
}
