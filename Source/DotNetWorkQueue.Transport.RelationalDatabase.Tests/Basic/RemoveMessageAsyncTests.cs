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
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic
{
    /// <summary>
    /// The shared <see cref="RemoveMessage{T}"/> that LiteDb and SQLite both resolve.
    ///
    /// It holds a synchronous and an asynchronous delete handler, and which one runs matters more than
    /// the result: the asynchronous consumer's commit bottoms out here, on the per-message happy path,
    /// so a blocking delete occupies a thread-pool thread for every message processed. Both paths
    /// return the same status, so nothing but a direct assertion would catch the wrong one.
    /// </summary>
    [TestClass]
    public class RemoveMessageAsyncTests
    {
        [TestMethod]
        public async Task RemoveAsync_UsesTheAsynchronousDeleteHandler()
        {
            var harness = new Harness(deleted: 1);

            var status = await harness.Sut.RemoveAsync(harness.MessageId, RemoveMessageReason.Complete)
                .ConfigureAwait(false);

            Assert.AreEqual(RemoveMessageStatus.Removed, status);
            await harness.Async.Received(1).HandleAsync(Arg.Any<DeleteMessageCommand<long>>()).ConfigureAwait(false);
            harness.Sync.DidNotReceiveWithAnyArgs().Handle(null);
        }

        [TestMethod]
        public async Task RemoveAsync_WhenNothingWasDeleted_ReportsNotFound()
        {
            var harness = new Harness(deleted: 0);

            var status = await harness.Sut.RemoveAsync(harness.MessageId, RemoveMessageReason.Complete)
                .ConfigureAwait(false);

            Assert.AreEqual(RemoveMessageStatus.NotFound, status);
        }

        [TestMethod]
        public async Task RemoveAsync_WithNoMessageId_ReportsNotFoundWithoutTouchingTheTransport()
        {
            var harness = new Harness(deleted: 1, hasMessageId: false);

            var status = await harness.Sut.RemoveAsync(harness.MessageId, RemoveMessageReason.Complete)
                .ConfigureAwait(false);

            Assert.AreEqual(RemoveMessageStatus.NotFound, status);
            await harness.Async.DidNotReceiveWithAnyArgs().HandleAsync(null).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RemoveAsync_FromAContext_CarriesTheContextsMessageId()
        {
            var harness = new Harness(deleted: 1);
            var context = Substitute.For<IMessageContext>();
            context.MessageId.Returns(harness.MessageId);

            var status = await harness.Sut.RemoveAsync(context, RemoveMessageReason.Complete)
                .ConfigureAwait(false);

            Assert.AreEqual(RemoveMessageStatus.Removed, status);
            await harness.Async.Received(1)
                .HandleAsync(Arg.Is<DeleteMessageCommand<long>>(c => c.QueueId == Harness.Id))
                .ConfigureAwait(false);
        }

        private sealed class Harness
        {
            public const long Id = 7L;

            public RemoveMessage<long> Sut { get; }
            public ICommandHandlerWithOutput<DeleteMessageCommand<long>, long> Sync { get; }
            public ICommandHandlerWithOutputAsync<DeleteMessageCommand<long>, long> Async { get; }
            public IMessageId MessageId { get; }

            public Harness(long deleted, bool hasMessageId = true)
            {
                Sync = Substitute.For<ICommandHandlerWithOutput<DeleteMessageCommand<long>, long>>();
                Async = Substitute.For<ICommandHandlerWithOutputAsync<DeleteMessageCommand<long>, long>>();
                Async.HandleAsync(Arg.Any<DeleteMessageCommand<long>>()).Returns(Task.FromResult(deleted));

                var setting = Substitute.For<ISetting>();
                setting.Value.Returns(Id);
                MessageId = Substitute.For<IMessageId>();
                MessageId.HasValue.Returns(hasMessageId);
                MessageId.Id.Returns(setting);

                Sut = new RemoveMessage<long>(Sync, Async);
            }
        }
    }
}
