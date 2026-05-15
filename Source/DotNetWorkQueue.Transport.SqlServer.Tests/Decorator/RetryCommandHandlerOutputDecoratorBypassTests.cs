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
using System.Data.Common;
using System.Threading.Tasks;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.SqlServer.Decorator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Decorator
{
    /// <summary>
    /// Verifies the IRetrySkippable bypass branch on the SqlServer retry decorator.
    /// When a command implements IRetrySkippable with SkipRetry == true (the production
    /// case: a RelationalSendMessageCommand carrying a caller-supplied DbTransaction),
    /// the decorator must invoke the inner handler exactly once WITHOUT consulting the
    /// Polly pipeline registry.
    /// </summary>
    [TestClass]
    public class RetryCommandHandlerOutputDecoratorBypassTests
    {
        private static RelationalSendMessageCommand BuildCommandWithTx()
        {
            var msg = Substitute.For<IMessage>();
            var data = new AdditionalMessageData();
            var transaction = Substitute.For<DbTransaction>();
            return new RelationalSendMessageCommand(msg, data, transaction);
        }

        [TestMethod]
        public void Handle_WhenCommandSkipsRetry_InvokesInnerOnce_AndDoesNotAccessRegistry()
        {
            var decorated = Substitute.For<ICommandHandlerWithOutput<SendMessageCommand, long>>();
            decorated.Handle(Arg.Any<SendMessageCommand>()).Returns(42L);
            var policies = Substitute.For<IPolicies>();

            var sut = new RetryCommandHandlerOutputDecorator<SendMessageCommand, long>(decorated, policies);
            var command = BuildCommandWithTx();

            var result = sut.Handle(command);

            Assert.AreEqual(42L, result);
            decorated.Received(1).Handle(command);
            // Property-getter assertion pattern (Phase 1 SUMMARY-1.1 — Registry is the
            // sealed ResiliencePipelineRegistry<string> and is unmockable; assert the
            // getter was never read instead).
            _ = policies.DidNotReceiveWithAnyArgs().Registry;
        }

        [TestMethod]
        public async Task HandleAsync_WhenCommandSkipsRetry_InvokesInnerOnce_AndDoesNotAccessRegistry()
        {
            var decorated = Substitute.For<ICommandHandlerWithOutputAsync<SendMessageCommand, long>>();
            decorated.HandleAsync(Arg.Any<SendMessageCommand>()).Returns(Task.FromResult(42L));
            var policies = Substitute.For<IPolicies>();

            var sut = new RetryCommandHandlerOutputDecoratorAsync<SendMessageCommand, long>(decorated, policies);
            var command = BuildCommandWithTx();

            var result = await sut.HandleAsync(command);

            Assert.AreEqual(42L, result);
            await decorated.Received(1).HandleAsync(command);
            _ = policies.DidNotReceiveWithAnyArgs().Registry;
        }
    }
}
