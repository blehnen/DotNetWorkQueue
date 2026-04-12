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
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandHandler;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic.CommandHandler
{
    [TestClass]
    public class SetJobLastKnownEventCommandHandlerTests
    {
        [TestMethod]
        public void Constructor_NullCommandCache_Throws()
        {
            var dbConnectionFactory = Substitute.For<IDbConnectionFactory>();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new SetJobLastKnownEventCommandHandler(null, dbConnectionFactory));
        }

        [TestMethod]
        public void Constructor_NullDbConnectionFactory_Throws()
        {
            var commandCache = CreateCommandCache();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new SetJobLastKnownEventCommandHandler(commandCache, null));
        }

        [TestMethod]
        public void Constructor_ValidArgs_Succeeds()
        {
            var commandCache = CreateCommandCache();
            var dbConnectionFactory = Substitute.For<IDbConnectionFactory>();
            var handler = new SetJobLastKnownEventCommandHandler(commandCache, dbConnectionFactory);
            Assert.IsNotNull(handler);
        }

        [TestMethod]
        public void Handle_CommandCache_ProducesNonEmptySetJobLastKnownEventSql()
        {
            // The Handle() path executes (NpgsqlConnection)_dbConnectionFactory.Create() and then
            // calls non-virtual instance methods on the concrete NpgsqlConnection. NpgsqlConnection
            // is SEALED, so NSubstitute (Castle DynamicProxy) cannot create a substitute for it
            // ("parent type is sealed" TypeLoadException). That makes a true unit test of the
            // open/execute lifecycle impossible without altering the refactor to return IDbConnection
            // from the factory and remove the cast. Until then, the integration test suite covers
            // the live PostgreSQL execution path.
            //
            // We assert here only that the command cache the handler depends on yields a populated
            // SQL string for the SetJobLastKnownEvent key, which is the one observable contract we
            // can validate without an in-process Postgres instance.
            var commandCache = CreateCommandCache();
            var sql = commandCache.GetCommand(CommandStringTypes.SetJobLastKnownEvent);
            Assert.IsFalse(string.IsNullOrWhiteSpace(sql),
                "PostgreSqlCommandStringCache should return non-empty SQL for SetJobLastKnownEvent.");
        }

        private static PostgreSqlCommandStringCache CreateCommandCache()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var connection = fixture.Create<IConnectionInformation>();
            connection.QueueName.Returns("TestQueue");
            fixture.Inject(connection);
            return fixture.Create<PostgreSqlCommandStringCache>();
        }
    }
}
