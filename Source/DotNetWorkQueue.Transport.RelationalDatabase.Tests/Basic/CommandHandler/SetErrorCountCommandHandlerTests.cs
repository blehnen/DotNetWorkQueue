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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.RelationalDatabase.Tests.TestHelpers;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.CommandHandler
{
    /// <summary>
    /// Counting how many times a message has failed with a given exception type.
    ///
    /// This was a check-then-write across two connections with nothing enforcing one row per
    /// (QueueID, ExceptionType), so two failures of the same message arriving together could each see no
    /// row and each insert one. The retry count then reads lower than reality and the message gets more
    /// attempts than configured - a poison message can loop instead of reaching the error queue.
    ///
    /// New queues get a unique index and a single atomic statement. Queues created before that index
    /// existed do not have it, and the library does not upgrade schemas, so the old path has to keep
    /// working - which is what these cover.
    /// </summary>
    [TestClass]
    public class SetErrorCountCommandHandlerTests
    {
        [TestMethod]
        public void Handle_WithTheUniqueIndex_UsesTheSingleStatement()
        {
            var h = new Harness(indexExists: true);

            h.Handler.Handle(new SetErrorCountCommand<long>("System.Exception", 42));

            h.PrepareCommand.Received(1).Handle(Arg.Any<SetErrorCountCommand<long>>(), Arg.Any<System.Data.Common.DbCommand>(),
                CommandStringTypes.UpsertErrorCount);
            h.ExistsQuery.DidNotReceiveWithAnyArgs().Handle(null);
        }

        [TestMethod]
        public void Handle_WithoutTheUniqueIndex_FallsBackToCheckThenWrite()
        {
            //a queue created before the index existed; the old path has to still count errors
            var h = new Harness(indexExists: false, recordExists: false);

            h.Handler.Handle(new SetErrorCountCommand<long>("System.Exception", 42));

            h.PrepareCommand.Received(1).Handle(Arg.Any<SetErrorCountCommand<long>>(), Arg.Any<System.Data.Common.DbCommand>(),
                CommandStringTypes.InsertErrorCount);
        }

        [TestMethod]
        public void Handle_WithoutTheUniqueIndex_UpdatesAnExistingRow()
        {
            var h = new Harness(indexExists: false, recordExists: true);

            h.Handler.Handle(new SetErrorCountCommand<long>("System.Exception", 42));

            h.PrepareCommand.Received(1).Handle(Arg.Any<SetErrorCountCommand<long>>(), Arg.Any<System.Data.Common.DbCommand>(),
                CommandStringTypes.UpdateErrorCount);
        }

        [TestMethod]
        public void TheSchemaIsOnlyAskedAboutOnce()
        {
            //the schema does not change underneath a running consumer, and this is on the failure path
            var h = new Harness(indexExists: true);

            h.Handler.Handle(new SetErrorCountCommand<long>("System.Exception", 42));
            h.Handler.Handle(new SetErrorCountCommand<long>("System.Exception", 42));
            h.Handler.Handle(new SetErrorCountCommand<long>("System.Exception", 43));

            h.IndexQuery.Received(1).Handle(Arg.Any<GetErrorTrackingUniqueIndexExistsQuery>());
        }

        private sealed class Harness
        {
            public SetErrorCountCommandHandler<long> Handler { get; }
            public IPrepareCommandHandler<SetErrorCountCommand<long>> PrepareCommand { get; }
            public IQueryHandler<GetErrorRecordExistsQuery<long>, bool> ExistsQuery { get; }
            public IQueryHandler<GetErrorTrackingUniqueIndexExistsQuery, bool> IndexQuery { get; }

            public Harness(bool indexExists, bool recordExists = false)
            {
                var fixture = AdoNetMockFixture.Create();

                ExistsQuery = Substitute.For<IQueryHandler<GetErrorRecordExistsQuery<long>, bool>>();
                ExistsQuery.Handle(Arg.Any<GetErrorRecordExistsQuery<long>>()).Returns(recordExists);

                IndexQuery = Substitute.For<IQueryHandler<GetErrorTrackingUniqueIndexExistsQuery, bool>>();
                IndexQuery.Handle(Arg.Any<GetErrorTrackingUniqueIndexExistsQuery>()).Returns(indexExists);

                PrepareCommand = Substitute.For<IPrepareCommandHandler<SetErrorCountCommand<long>>>();

                var tableNames = Substitute.For<ITableNameHelper>();
                tableNames.ErrorTrackingName.Returns("ErrorTracking");

                Handler = new SetErrorCountCommandHandler<long>(
                    ExistsQuery,
                    Substitute.For<IQueryHandlerAsync<GetErrorRecordExistsQuery<long>, bool>>(),
                    fixture.ConnectionFactory,
                    PrepareCommand,
                    IndexQuery,
                    tableNames);
            }
        }
    }
}
