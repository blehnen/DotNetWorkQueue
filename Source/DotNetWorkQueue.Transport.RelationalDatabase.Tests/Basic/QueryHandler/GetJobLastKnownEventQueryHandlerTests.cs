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
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler;
using DotNetWorkQueue.Transport.RelationalDatabase.Tests.TestHelpers;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryHandler
{
    [TestClass]
    public class GetJobLastKnownEventQueryHandlerTests
    {
        [TestMethod]
        public void Handle_ReaderHasRow_ReturnsReadColumnValue()
        {
            var (handler, fixture, prepareQuery) = CreateHandler();
            var expected = new DateTimeOffset(2026, 4, 12, 10, 30, 0, TimeSpan.Zero);
            fixture.Reader.Read().Returns(true, false);
            fixture.ReadColumn.ReadAsDateTimeOffset(CommandStringTypes.GetJobLastKnownEvent, 0, fixture.Reader)
                .Returns(expected);

            var query = new GetJobLastKnownEventQuery("jobName");

            var result = handler.Handle(query);

            Assert.AreEqual(expected, result);
            fixture.ConnectionFactory.Received(1).Create();
            fixture.Connection.Received(1).Open();
            prepareQuery.Received(1).Handle(query, fixture.Command, CommandStringTypes.GetJobLastKnownEvent);
            fixture.ReadColumn.Received(1).ReadAsDateTimeOffset(CommandStringTypes.GetJobLastKnownEvent, 0, fixture.Reader);
        }

        [TestMethod]
        public void Handle_ReaderHasNoRows_ReturnsDefault()
        {
            var (handler, fixture, prepareQuery) = CreateHandler();
            fixture.Reader.Read().Returns(false);

            var query = new GetJobLastKnownEventQuery("jobName");

            var result = handler.Handle(query);

            Assert.AreEqual(default(DateTimeOffset), result);
            fixture.Connection.Received(1).Open();
            prepareQuery.Received(1).Handle(query, fixture.Command, CommandStringTypes.GetJobLastKnownEvent);
            fixture.ReadColumn.DidNotReceive()
                .ReadAsDateTimeOffset(Arg.Any<CommandStringTypes>(), Arg.Any<int>(), Arg.Any<IDataReader>());
        }

        [TestMethod]
        public void Constructor_NullPrepareQuery_Throws()
        {
            var dbConnectionFactory = Substitute.For<IDbConnectionFactory>();
            var readColumn = Substitute.For<IReadColumn>();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new GetJobLastKnownEventQueryHandler(null, dbConnectionFactory, readColumn));
        }

        [TestMethod]
        public void Constructor_NullDbConnectionFactory_Throws()
        {
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetJobLastKnownEventQuery, DateTimeOffset>>();
            var readColumn = Substitute.For<IReadColumn>();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new GetJobLastKnownEventQueryHandler(prepareQuery, null, readColumn));
        }

        [TestMethod]
        public void Constructor_NullReadColumn_Throws()
        {
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetJobLastKnownEventQuery, DateTimeOffset>>();
            var dbConnectionFactory = Substitute.For<IDbConnectionFactory>();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new GetJobLastKnownEventQueryHandler(prepareQuery, dbConnectionFactory, null));
        }

        private static (GetJobLastKnownEventQueryHandler handler,
                        AdoNetMockFixture fixture,
                        IPrepareQueryHandler<GetJobLastKnownEventQuery, DateTimeOffset> prepareQuery) CreateHandler()
        {
            var fixture = AdoNetMockFixture.Create();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetJobLastKnownEventQuery, DateTimeOffset>>();
            var handler = new GetJobLastKnownEventQueryHandler(prepareQuery, fixture.ConnectionFactory, fixture.ReadColumn);
            return (handler, fixture, prepareQuery);
        }
    }
}
