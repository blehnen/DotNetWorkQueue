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
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetWorkQueue.Trace.Decorator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests.Trace.Decorator
{
    /// <summary>
    /// The tracing decorator over message removal.
    ///
    /// It looked headers up before checking that there was an id to look them up by. Redis' header
    /// reader dereferences the id unconditionally, so tracing turned the one input every
    /// implementation answers with <see cref="RemoveMessageStatus.NotFound"/> into a crash - and the
    /// null guards inside those implementations were unreachable whenever tracing was on.
    /// </summary>
    [TestClass]
    public class RemoveMessageDecoratorTests
    {
        [TestMethod]
        public void Remove_WithNoId_DoesNotAskForHeaders()
        {
            var harness = new Harness(hasId: false);

            var status = harness.Decorator.Remove(harness.MessageId, RemoveMessageReason.Complete);

            Assert.AreEqual(RemoveMessageStatus.NotFound, status);
            harness.GetHeader.DidNotReceiveWithAnyArgs().GetHeaders(null);
            harness.Decorated.Received(1).Remove(harness.MessageId, RemoveMessageReason.Complete);
        }

        [TestMethod]
        public async Task RemoveAsync_WithNoId_DoesNotAskForHeaders()
        {
            var harness = new Harness(hasId: false);

            var status = await harness.Decorator
                .RemoveAsync(harness.MessageId, RemoveMessageReason.Complete).ConfigureAwait(false);

            Assert.AreEqual(RemoveMessageStatus.NotFound, status);
            harness.GetHeader.DidNotReceiveWithAnyArgs().GetHeaders(null);
            await harness.Decorated.Received(1)
                .RemoveAsync(harness.MessageId, RemoveMessageReason.Complete).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RemoveAsync_WithAnId_StillReadsHeadersToParentTheSpan()
        {
            var harness = new Harness(hasId: true);

            var status = await harness.Decorator
                .RemoveAsync(harness.MessageId, RemoveMessageReason.Expired).ConfigureAwait(false);

            Assert.AreEqual(RemoveMessageStatus.Removed, status);
            harness.GetHeader.Received(1).GetHeaders(harness.MessageId);
        }

        [TestMethod]
        public async Task RemoveAsync_FromAContext_ReachesTheDecoratedHandler()
        {
            var harness = new Harness(hasId: true);
            var context = Substitute.For<IMessageContext>();

            await harness.Decorator.RemoveAsync(context, RemoveMessageReason.Complete).ConfigureAwait(false);

            await harness.Decorated.Received(1)
                .RemoveAsync(context, RemoveMessageReason.Complete).ConfigureAwait(false);
        }

        private sealed class Harness
        {
            public RemoveMessageDecorator Decorator { get; }
            public IRemoveMessage Decorated { get; }
            public IGetHeader GetHeader { get; }
            public IMessageId MessageId { get; }

            public Harness(bool hasId)
            {
                Decorated = Substitute.For<IRemoveMessage>();
                Decorated.Remove(Arg.Any<IMessageId>(), Arg.Any<RemoveMessageReason>())
                    .Returns(hasId ? RemoveMessageStatus.Removed : RemoveMessageStatus.NotFound);
                Decorated.RemoveAsync(Arg.Any<IMessageId>(), Arg.Any<RemoveMessageReason>())
                    .Returns(Task.FromResult(hasId ? RemoveMessageStatus.Removed : RemoveMessageStatus.NotFound));

                var setting = Substitute.For<ISetting>();
                setting.Value.Returns(1L);
                MessageId = Substitute.For<IMessageId>();
                MessageId.HasValue.Returns(hasId);
                MessageId.Id.Returns(setting);

                GetHeader = Substitute.For<IGetHeader>();
                GetHeader.GetHeaders(Arg.Any<IMessageId>()).Returns(new Dictionary<string, object>());

                Decorator = new RemoveMessageDecorator(Decorated,
                    new ActivitySource("DotNetWorkQueue.Tests.Remove"),
                    Substitute.For<IStandardHeaders>(), GetHeader);
            }
        }
    }
}
