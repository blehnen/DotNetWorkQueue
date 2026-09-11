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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler;
using DotNetWorkQueue.Transport.RelationalDatabase.Tests.TestHelpers;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.CommandHandler
{
    /// <summary>
    /// The delete has to report whether the message was actually there.
    ///
    /// It returned a hardcoded 1, and `RemoveMessage.Remove` turns anything above zero into
    /// <c>Removed</c> - so an id with no queue row, already removed or never present, was reported as
    /// removed. Callers that act on the status rather than ignoring it saw the wrong answer:
    /// `ClearExpiredMessages` and `ClearErrorMessages` count what they removed, and the dashboard
    /// delete shows the result to a user.
    ///
    /// The queue row is the authoritative one; the meta data, status and error rows are derived from it.
    /// </summary>
    [TestClass]
    public class DeleteMessageCommandHandlerTests
    {
        [TestMethod]
        public void Handle_WhenTheQueueRowWasThere_ReportsOneRemoved()
        {
            var handler = Create(out var fixture, statusTable: false);
            //the second statement is the queue row; the first is the meta data
            fixture.Command.ExecuteNonQuery().Returns(0, 1, 0, 0, 0);

            Assert.AreEqual(1, handler.Handle(new DeleteMessageCommand<long>(42)));
        }

        [TestMethod]
        public void Handle_WhenTheQueueRowWasNotThere_ReportsNothingRemoved()
        {
            var handler = Create(out var fixture, statusTable: false);
            fixture.Command.ExecuteNonQuery().Returns(0, 0, 0, 0, 0);

            Assert.AreEqual(0, handler.Handle(new DeleteMessageCommand<long>(42)),
                "a message that was not there was reported as removed");
        }

        [TestMethod]
        public void Handle_WithAStatusTable_StillReportsTheQueueRow()
        {
            //the status delete runs after the queue delete and must not become the answer
            var handler = Create(out var fixture, statusTable: true);
            fixture.Command.ExecuteNonQuery().Returns(0, 1, 0, 0, 0);

            Assert.AreEqual(1, handler.Handle(new DeleteMessageCommand<long>(42)));
        }

        private static DeleteMessageCommandHandler Create(out AdoNetMockFixture fixture, bool statusTable)
        {
            fixture = AdoNetMockFixture.Create(withTransaction: true);

            var transportOptions = Substitute.For<ITransportOptions>();
            transportOptions.EnableStatusTable.Returns(statusTable);
            var optionsFactory = Substitute.For<ITransportOptionsFactory>();
            optionsFactory.Create().Returns(transportOptions);

            return new DeleteMessageCommandHandler(optionsFactory, fixture.ConnectionFactory,
                fixture.TransactionFactory, Substitute.For<IPrepareCommandHandler<DeleteMessageCommand<long>>>());
        }
    }
}
