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
        private const string SourceName = "DotNetWorkQueue.Tests.Remove";
        private ActivityListener _listener;

        /// <summary>The RemovedBecause tag of the last span the decorator finished.</summary>
        private string _lastRemovedBecause;

        /// <summary>
        /// Without a listener every StartActivity returns null, the null-conditionals below it are all
        /// skipped, and the decorator's tagging never executes in any test - so nothing here would
        /// notice a span losing its tags. Sampling makes the scopes real, and ActivityStopped is the
        /// only point the decorator's activity is reachable from outside the using block that made it.
        /// </summary>
        [TestInitialize]
        public void Listen()
        {
            _lastRemovedBecause = null;
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
                ActivityStopped = a => _lastRemovedBecause = a.GetTagItem("RemovedBecause") as string
            };
            ActivitySource.AddActivityListener(_listener);
        }

        [TestCleanup]
        public void StopListening() => _listener?.Dispose();

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
            Assert.AreEqual(nameof(RemoveMessageReason.Expired), _lastRemovedBecause,
                "the span did not carry the reason the message was removed");
        }

        [TestMethod]
        public void Remove_WithAnId_StillReadsHeadersToParentTheSpan()
        {
            //the synchronous twin of the async case below it: both branches of the id overload have to
            //be exercised, or the header lookup only ever runs on one of the two paths a caller can take
            var harness = new Harness(hasId: true);

            var status = harness.Decorator.Remove(harness.MessageId, RemoveMessageReason.Expired);

            Assert.AreEqual(RemoveMessageStatus.Removed, status);
            harness.GetHeader.Received(1).GetHeaders(harness.MessageId);
            harness.Decorated.Received(1).Remove(harness.MessageId, RemoveMessageReason.Expired);
            Assert.AreEqual(nameof(RemoveMessageReason.Expired), _lastRemovedBecause,
                "the span did not carry the reason the message was removed");
        }

        [TestMethod]
        public void Remove_FromAContext_ReachesTheDecoratedHandler()
        {
            var harness = new Harness(hasId: true);
            var context = Substitute.For<IMessageContext>();

            harness.Decorator.Remove(context, RemoveMessageReason.Complete);

            harness.Decorated.Received(1).Remove(context, RemoveMessageReason.Complete);
            //the context overload traces from the context rather than from a header lookup
            harness.GetHeader.DidNotReceiveWithAnyArgs().GetHeaders(null);
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

                Decorator = new RemoveMessageDecorator(Decorated, new ActivitySource(SourceName),
                    Substitute.For<IStandardHeaders>(), GetHeader);
            }
        }
    }
}
