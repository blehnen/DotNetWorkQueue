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
    /// Counting how many times a message has failed with a given exception type.
    ///
    /// This was once a check-then-write across two connections with nothing enforcing one row per
    /// (QueueID, ExceptionType), so two failures of the same message arriving together could each see
    /// no row and each insert one. The retry count then read lower than reality and the message got
    /// more attempts than configured - a poison message could loop instead of reaching the error queue.
    ///
    /// Every queue now carries the unique index that makes one statement enough: new queues are
    /// created with it, and an older one gains it at schema version 1, which a producer or consumer
    /// refuses to start without. The fallback, and the schema look-up that chose between the two, are
    /// gone (GitHub #308) - so what is left to hold is that both paths issue the one statement and
    /// nothing else.
    /// </summary>
    [TestClass]
    public class SetErrorCountCommandHandlerTests
    {
        [TestMethod]
        public void Handle_UsesTheSingleStatement()
        {
            var h = new Harness();

            h.Handler.Handle(new SetErrorCountCommand<long>("System.Exception", 42, 1));

            h.PrepareCommand.Received(1).Handle(Arg.Any<SetErrorCountCommand<long>>(),
                Arg.Any<System.Data.Common.DbCommand>(), CommandStringTypes.UpsertErrorCount);
        }

        [TestMethod]
        public async Task HandleAsync_UsesTheSingleStatement()
        {
            //the async twin has gone wrong on its own before, so it is asserted rather than assumed
            var h = new Harness();

            await h.Handler.HandleAsync(new SetErrorCountCommand<long>("System.Exception", 42, 1));

            h.PrepareCommand.Received(1).Handle(Arg.Any<SetErrorCountCommand<long>>(),
                Arg.Any<System.Data.Common.DbCommand>(), CommandStringTypes.UpsertErrorCount);
        }

        [TestMethod]
        public void Handle_TakesOneConnectionAndOneCommand()
        {
            //it used to take a second connection to ask what the schema looked like, on the failure
            //path, where every concurrent first failure held one connection while waiting for another
            var h = new Harness();

            h.Handler.Handle(new SetErrorCountCommand<long>("System.Exception", 42, 1));

            h.ConnectionFactory.Received(1).Create();
            h.Connection.Received(1).Open();
        }

        private sealed class Harness
        {
            public SetErrorCountCommandHandler<long> Handler { get; }
            public IPrepareCommandHandler<SetErrorCountCommand<long>> PrepareCommand { get; }
            public IDbConnectionFactory ConnectionFactory { get; }
            public System.Data.Common.DbConnection Connection { get; }

            public Harness()
            {
                var fixture = AdoNetMockFixture.Create();
                ConnectionFactory = fixture.ConnectionFactory;
                Connection = fixture.Connection;

                PrepareCommand = Substitute.For<IPrepareCommandHandler<SetErrorCountCommand<long>>>();

                Handler = new SetErrorCountCommandHandler<long>(fixture.ConnectionFactory, PrepareCommand);
            }
        }
    }
}
