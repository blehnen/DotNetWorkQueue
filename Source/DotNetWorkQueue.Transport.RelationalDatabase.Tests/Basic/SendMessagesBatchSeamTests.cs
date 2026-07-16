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
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic
{
    /// <summary>
    /// Verifies the dispatch seam in <see cref="SendMessages{T}"/>: a registered (non-marker)
    /// batch handler is dispatched to, while the no-op marker handler falls back to the
    /// per-message loop. Lives here because there is no Transport.Shared test project; this
    /// project references Transport.Shared transitively.
    /// </summary>
    [TestClass]
    public class SendMessagesBatchSeamTests
    {
        private static List<QueueMessage<IMessage, IAdditionalMessageData>> BuildMessages(int count)
        {
            var list = new List<QueueMessage<IMessage, IAdditionalMessageData>>(count);
            for (var i = 0; i < count; i++)
            {
                list.Add(new QueueMessage<IMessage, IAdditionalMessageData>(
                    Substitute.For<IMessage>(), Substitute.For<IAdditionalMessageData>()));
            }
            return list;
        }

        private static (ISentMessageFactory sentFactory,
            ICommandHandlerWithOutput<SendMessageCommand, long> single,
            ICommandHandlerWithOutputAsync<SendMessageCommand, long> singleAsync) BuildSingleHandlers()
        {
            var sentFactory = Substitute.For<ISentMessageFactory>();
            var single = Substitute.For<ICommandHandlerWithOutput<SendMessageCommand, long>>();
            var singleAsync = Substitute.For<ICommandHandlerWithOutputAsync<SendMessageCommand, long>>();
            single.Handle(Arg.Any<SendMessageCommand>()).Returns(1L);
            singleAsync.HandleAsync(Arg.Any<SendMessageCommand>()).Returns(Task.FromResult(1L));
            return (sentFactory, single, singleAsync);
        }

        [TestMethod]
        public void Send_WithRealBatchHandler_DispatchesToBatchHandler()
        {
            var (sentFactory, single, singleAsync) = BuildSingleHandlers();
            var batch = Substitute.For<ICommandHandlerWithOutput<SendMessageCommandBatch, QueueOutputMessages>>();
            var batchAsync = Substitute.For<ICommandHandlerWithOutputAsync<SendMessageCommandBatch, QueueOutputMessages>>();
            var expected = new QueueOutputMessages(new List<IQueueOutputMessage>());
            batch.Handle(Arg.Any<SendMessageCommandBatch>()).Returns(expected);

            var sut = new SendMessages<long>(sentFactory, single, singleAsync, batch, batchAsync, new SendMessageBatchSupport(true));
            var result = sut.Send(BuildMessages(3));

            Assert.AreSame(expected, result);
            batch.Received(1).Handle(Arg.Any<SendMessageCommandBatch>());
            single.DidNotReceive().Handle(Arg.Any<SendMessageCommand>());
        }

        [TestMethod]
        public void Send_WithNoOpHandler_FallsBackToPerMessageLoop()
        {
            var (sentFactory, single, singleAsync) = BuildSingleHandlers();
            var noop = new NoOpSendMessageCommandBatchHandler();

            var sut = new SendMessages<long>(sentFactory, single, singleAsync, noop, noop, new SendMessageBatchSupport(false));
            var messages = BuildMessages(3);
            sut.Send(messages);

            single.Received(messages.Count).Handle(Arg.Any<SendMessageCommand>());
        }

        [TestMethod]
        public async Task SendAsync_WithRealBatchHandler_DispatchesToBatchHandler()
        {
            var (sentFactory, single, singleAsync) = BuildSingleHandlers();
            var batch = Substitute.For<ICommandHandlerWithOutput<SendMessageCommandBatch, QueueOutputMessages>>();
            var batchAsync = Substitute.For<ICommandHandlerWithOutputAsync<SendMessageCommandBatch, QueueOutputMessages>>();
            var expected = new QueueOutputMessages(new List<IQueueOutputMessage>());
            batchAsync.HandleAsync(Arg.Any<SendMessageCommandBatch>()).Returns(Task.FromResult(expected));

            var sut = new SendMessages<long>(sentFactory, single, singleAsync, batch, batchAsync, new SendMessageBatchSupport(true));
            var result = await sut.SendAsync(BuildMessages(3));

            Assert.AreSame(expected, result);
            await batchAsync.Received(1).HandleAsync(Arg.Any<SendMessageCommandBatch>());
            await singleAsync.DidNotReceive().HandleAsync(Arg.Any<SendMessageCommand>());
        }

        [TestMethod]
        public async Task SendAsync_WithNoOpHandler_FallsBackToPerMessageLoop()
        {
            var (sentFactory, single, singleAsync) = BuildSingleHandlers();
            var noop = new NoOpSendMessageCommandBatchHandler();

            var sut = new SendMessages<long>(sentFactory, single, singleAsync, noop, noop, new SendMessageBatchSupport(false));
            var messages = BuildMessages(3);
            await sut.SendAsync(messages);

            await singleAsync.Received(messages.Count).HandleAsync(Arg.Any<SendMessageCommand>());
        }
    }
}
