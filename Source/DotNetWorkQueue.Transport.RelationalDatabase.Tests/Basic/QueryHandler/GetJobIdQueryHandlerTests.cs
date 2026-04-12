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
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryHandler
{
    [TestClass]
    public class GetJobIdQueryHandlerTests
    {
        [TestMethod]
        public void Handle_ReaderHasRow_ReturnsReadColumnValue()
        {
            var fixture = CreateFixture();
            fixture.Reader.Read().Returns(true, false);
            fixture.ReadColumn.ReadAsType<long>(CommandStringTypes.GetJobId, 0, fixture.Reader)
                .Returns(42L);

            var query = new GetJobIdQuery<long>("jobName");

            var result = fixture.Handler.Handle(query);

            Assert.AreEqual(42L, result);
            fixture.DbConnectionFactory.Received(1).Create();
            fixture.Connection.Received(1).Open();
            fixture.PrepareQuery.Received(1).Handle(query, fixture.Command, CommandStringTypes.GetJobId);
            fixture.ReadColumn.Received(1).ReadAsType<long>(CommandStringTypes.GetJobId, 0, fixture.Reader);
        }

        [TestMethod]
        public void Handle_ReaderHasNoRows_ReturnsDefault()
        {
            var fixture = CreateFixture();
            fixture.Reader.Read().Returns(false);

            var query = new GetJobIdQuery<long>("jobName");

            var result = fixture.Handler.Handle(query);

            Assert.AreEqual(default(long), result);
            fixture.Connection.Received(1).Open();
            fixture.PrepareQuery.Received(1).Handle(query, fixture.Command, CommandStringTypes.GetJobId);
            fixture.ReadColumn.DidNotReceive()
                .ReadAsType<long>(Arg.Any<CommandStringTypes>(), Arg.Any<int>(), Arg.Any<IDataReader>());
        }

        [TestMethod]
        public void Constructor_NullPrepareQuery_Throws()
        {
            var dbConnectionFactory = Substitute.For<IDbConnectionFactory>();
            var readColumn = Substitute.For<IReadColumn>();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new GetJobIdQueryHandler<long>(null, dbConnectionFactory, readColumn));
        }

        [TestMethod]
        public void Constructor_NullDbConnectionFactory_Throws()
        {
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetJobIdQuery<long>, long>>();
            var readColumn = Substitute.For<IReadColumn>();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new GetJobIdQueryHandler<long>(prepareQuery, null, readColumn));
        }

        [TestMethod]
        public void Constructor_NullReadColumn_Throws()
        {
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetJobIdQuery<long>, long>>();
            var dbConnectionFactory = Substitute.For<IDbConnectionFactory>();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new GetJobIdQueryHandler<long>(prepareQuery, dbConnectionFactory, null));
        }

        private TestFixture CreateFixture()
        {
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetJobIdQuery<long>, long>>();
            var dbConnectionFactory = Substitute.For<IDbConnectionFactory>();
            var readColumn = Substitute.For<IReadColumn>();

            var connection = Substitute.For<IDbConnection>();
            var command = Substitute.For<IDbCommand>();
            var reader = Substitute.For<IDataReader>();

            dbConnectionFactory.Create().Returns(connection);
            connection.CreateCommand().Returns(command);
            command.ExecuteReader().Returns(reader);

            var handler = new GetJobIdQueryHandler<long>(prepareQuery, dbConnectionFactory, readColumn);

            return new TestFixture
            {
                Handler = handler,
                PrepareQuery = prepareQuery,
                DbConnectionFactory = dbConnectionFactory,
                ReadColumn = readColumn,
                Connection = connection,
                Command = command,
                Reader = reader
            };
        }

        private class TestFixture
        {
            public GetJobIdQueryHandler<long> Handler { get; set; }
            public IPrepareQueryHandler<GetJobIdQuery<long>, long> PrepareQuery { get; set; }
            public IDbConnectionFactory DbConnectionFactory { get; set; }
            public IReadColumn ReadColumn { get; set; }
            public IDbConnection Connection { get; set; }
            public IDbCommand Command { get; set; }
            public IDataReader Reader { get; set; }
        }
    }
}
