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
//
// THROWAWAY SPIKE FILE — Phase 1 Polly decorator bypass PoC.
// Defines its own marker interface and a near-copy of
// RetryCommandHandlerOutputDecorator with the proposed IRetrySkippable
// early-return branch. Demonstrates the design works in compiled code
// without modifying any production decorator.
//
// Phase 2's first task DELETES this file and ships the production change.
//
using System;
using DotNetWorkQueue;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Polly.Registry;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Decorator
{
    /// <summary>
    /// Phase 1 throwaway spike. Verifies the proposed IRetrySkippable marker
    /// mechanism causes the relational retry decorator to bypass the Polly
    /// pipeline lookup and call the inner handler directly. Phase 2 deletes
    /// this file and adds the branch to the real
    /// <c>RetryCommandHandlerOutputDecorator</c>.
    /// </summary>
    [TestClass]
    public class _SpikePollyBypassPoC
    {
        /// <summary>
        /// Spike-local marker interface. Phase 2 introduces the real
        /// <c>DotNetWorkQueue.Transport.Shared.IRetrySkippable</c>; this PoC
        /// uses a private copy so the spike commit touches zero production
        /// code.
        /// </summary>
        internal interface _SpikeIRetrySkippable
        {
            bool SkipRetry { get; }
        }

        /// <summary>
        /// Local <see cref="SendMessageCommand"/> subclass that implements the
        /// spike marker. In Phase 2 this collapses into the real
        /// <c>SendMessageCommand</c> with
        /// <c>SkipRetry =&gt; ExternalTransaction != null</c>.
        /// </summary>
        private sealed class _SpikeSendCommand : SendMessageCommand, _SpikeIRetrySkippable
        {
            public _SpikeSendCommand(IMessage messageToSend, IAdditionalMessageData messageData, bool skipRetry)
                : base(messageToSend, messageData)
            {
                SkipRetry = skipRetry;
            }

            public bool SkipRetry { get; }
        }

        /// <summary>
        /// Inner handler that records how many times <see cref="Handle"/> ran.
        /// Stand-in for the real <c>SendMessageCommandHandler</c>.
        /// </summary>
        private sealed class _SpikeRecordingHandler : ICommandHandlerWithOutput<SendMessageCommand, long>
        {
            public int CallCount;

            public long Handle(SendMessageCommand command)
            {
                CallCount++;
                return 42L;
            }
        }

        /// <summary>
        /// Near-copy of
        /// <c>DotNetWorkQueue.Transport.SqlServer.Decorator.RetryCommandHandlerOutputDecorator&lt;TCommand, TOutput&gt;</c>
        /// with the proposed marker-bypass branch added at the top of
        /// <see cref="Handle"/>. The production decorator is NOT modified by
        /// this spike — that change lands in Phase 2.
        /// </summary>
        private sealed class _SpikePatchedRetryDecorator<TCommand, TOutput> : ICommandHandlerWithOutput<TCommand, TOutput>
        {
            private readonly ICommandHandlerWithOutput<TCommand, TOutput> _decorated;
            private readonly IPolicies _policies;

            public _SpikePatchedRetryDecorator(ICommandHandlerWithOutput<TCommand, TOutput> decorated, IPolicies policies)
            {
                Guard.NotNull(() => decorated, decorated);
                Guard.NotNull(() => policies, policies);

                _decorated = decorated;
                _policies = policies;
            }

            public TOutput Handle(TCommand command)
            {
                Guard.NotNull(() => command, command);

                // ----- proposed Phase 2 branch -----
                if (command is _SpikeIRetrySkippable skippable && skippable.SkipRetry)
                    return _decorated.Handle(command);
                // -----------------------------------

                ResiliencePipeline pipeline = null;
                try
                {
                    _policies.Registry.TryGetPipeline(TransportPolicyDefinitions.RetryCommandHandler, out pipeline);
                }
                catch (ObjectDisposedException)
                {
                    // Shutdown race — matches production decorator behavior.
                }

                if (pipeline != null)
                    return pipeline.Execute(_ => _decorated.Handle(command));
                return _decorated.Handle(command);
            }
        }

        private static SendMessageCommand BuildCommand(bool skipRetry)
        {
            var message = Substitute.For<IMessage>();
            var data = Substitute.For<IAdditionalMessageData>();
            return new _SpikeSendCommand(message, data, skipRetry);
        }

        [TestMethod]
        public void SkipRetry_When_CommandImplementsMarker_With_SkipRetryTrue()
        {
            var inner = new _SpikeRecordingHandler();
            var policies = Substitute.For<IPolicies>();

            var sut = new _SpikePatchedRetryDecorator<SendMessageCommand, long>(inner, policies);
            var command = BuildCommand(skipRetry: true);

            var result = sut.Handle(command);

            Assert.AreEqual(42L, result);
            Assert.AreEqual(1, inner.CallCount);
            // The bypass branch must short-circuit BEFORE touching the policies
            // registry. If a future refactor removes the early-return, this
            // assertion fires.
            _ = policies.DidNotReceiveWithAnyArgs().Registry;
        }

        [TestMethod]
        public void RetryPath_Still_Used_When_SkipRetryFalse()
        {
            var inner = new _SpikeRecordingHandler();
            var policies = Substitute.For<IPolicies>();
            using var registry = new ResiliencePipelineRegistry<string>();
            registry.TryAddBuilder(TransportPolicyDefinitions.RetryCommandHandler, (_, _) => { });
            policies.Registry.Returns(registry);

            var sut = new _SpikePatchedRetryDecorator<SendMessageCommand, long>(inner, policies);
            var command = BuildCommand(skipRetry: false);

            var result = sut.Handle(command);

            Assert.AreEqual(42L, result);
            Assert.AreEqual(1, inner.CallCount);
            // Negative case: pipeline lookup MUST happen when the marker says
            // do not skip. Asserts the property was read (TryGetPipeline is a
            // call on the concrete registry which NSubstitute can't intercept,
            // so we verify the getter was hit instead — which can only happen
            // on the non-bypass path).
            _ = policies.Received().Registry;
        }
    }
}
