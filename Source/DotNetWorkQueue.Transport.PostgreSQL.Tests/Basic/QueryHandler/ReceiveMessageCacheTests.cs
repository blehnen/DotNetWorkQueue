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
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Basic.QueryHandler;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using NSubstitute;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic.QueryHandler
{
    /// <summary>
    /// The de-queue statement is cached for routed and user-clause consumers too, not just the
    /// plain case. What the cache is keyed on has to cover everything that reaches the text.
    /// </summary>
    [TestClass]
    public class ReceiveMessageCacheTests
    {
        [TestMethod]
        public void Each_Route_Count_Gets_Its_Own_Statement()
        {
            //routes become @Route1..@RouteN placeholders, so the count is in the text and two
            //different counts must not share one cached statement
            var context = new Context();

            var one = context.Build(new List<string> { "a" });
            var two = context.Build(new List<string> { "a", "b" });
            var three = context.Build(new List<string> { "a", "b", "c" });

            Assert.AreNotEqual(one, two);
            Assert.AreNotEqual(two, three);
            Assert.Contains("@Route1", one);
            Assert.Contains("@Route2", two);
            Assert.Contains("@Route3", three);
        }

        [TestMethod]
        public void The_Same_Route_Count_Is_Served_From_The_Cache()
        {
            //cold then warm; different values, same count, so it is the same statement
            var context = new Context();

            var first = context.Build(new List<string> { "a", "b" });
            var second = context.Build(new List<string> { "x", "y" });

            Assert.AreEqual(first, second);
        }

        [TestMethod]
        public void No_Routes_Is_Not_Confused_With_Routes()
        {
            var context = new Context();

            var none = context.Build(null);
            var one = context.Build(new List<string> { "a" });

            Assert.AreNotEqual(none, one);
            Assert.DoesNotContain("@Route1", none);
        }

        [TestMethod]
        public void A_User_Clause_Still_Returns_Its_Parameters()
        {
            var context = new Context(additionalColumns: true);
            context.Configuration.AddUserParameter(new NpgsqlParameter("@p1", 1));
            context.Configuration.SetUserWhereClause("AND Col = @p1");

            var first = context.Build(null, out var firstParams);
            var second = context.Build(null, out var secondParams);

            Assert.AreEqual(first, second, "a fixed clause gives the same statement each time");
            Assert.IsNotNull(firstParams);
            Assert.IsNotNull(secondParams);
            Assert.HasCount(1, secondParams);
            Assert.AreEqual("@p1", secondParams[0].ParameterName);
        }

        [TestMethod]
        public void A_Changing_User_Clause_Is_Honoured_Every_Time()
        {
            //The reason a user clause is not cached at all. SetUserParametersAndClause takes a
            //factory that GetUserClause invokes on every de-queue, so the clause is free to differ
            //each time. Keying the cache on its text would both serve a stale statement and add a
            //permanent entry per poll until the process ran out of memory.
            var context = new Context(additionalColumns: true);
            var calls = 0;
            context.Configuration.SetUserParametersAndClause(
                () => new List<NpgsqlParameter> { new NpgsqlParameter("@p1", 1) },
                () => $"AND Col{++calls} = @p1");

            var first = context.Build(null);
            var second = context.Build(null);
            var third = context.Build(null);

            Assert.Contains("Col1", first);
            Assert.Contains("Col2", second);
            Assert.Contains("Col3", third);
            Assert.AreNotEqual(first, second);
            Assert.AreNotEqual(second, third);
        }

        [TestMethod]
        public void A_User_Clause_Is_Never_Cached()
        {
            //The actual regression test for the growth bug, and it has to assert on the cache
            //rather than on the returned text: a per-clause key produces a *different* key each
            //call, so a changing clause still returns correct SQL. What it does is add a permanent
            //entry every poll, for as long as the consumer lives.
            //
            //The literals below are the keys a per-clause implementation would have written. None
            //of them may exist, and the plain key must not be polluted either.
            var context = new Context(additionalColumns: true);
            var calls = 0;
            context.Configuration.SetUserParametersAndClause(
                () => new List<NpgsqlParameter> { new NpgsqlParameter("@p1", 1) },
                () => $"AND Col{++calls} = @p1");

            context.Build(null);
            context.Build(null);

            Assert.IsFalse(context.CacheContains("dequeueCommand|routes=0|user=AND Col1 = @p1"));
            Assert.IsFalse(context.CacheContains("dequeueCommand|routes=0|user=AND Col2 = @p1"));
            Assert.IsFalse(context.CacheContains("dequeueCommand"),
                "a user-clause statement must not be stored under the plain key either");
        }

        [TestMethod]
        public void Two_User_Clauses_Do_Not_Share_One_Statement()
        {
            var context = new Context(additionalColumns: true);
            context.Configuration.AddUserParameter(new NpgsqlParameter("@p1", 1));

            context.Configuration.SetUserWhereClause("AND ColA = @p1");
            var first = context.Build(null);

            context.Configuration.SetUserWhereClause("AND ColB = @p1");
            var second = context.Build(null);

            Assert.AreNotEqual(first, second);
            Assert.Contains("ColA", first);
            Assert.Contains("ColB", second);
        }

        private sealed class Context
        {
            private readonly PostgreSqlCommandStringCache _cache;
            private readonly ITableNameHelper _tableNameHelper;
            private readonly PostgreSqlMessageQueueTransportOptions _options;

            public QueueConsumerConfiguration Configuration { get; }

            public Context(bool additionalColumns = false)
            {
                var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
                Configuration = fixture.Create<QueueConsumerConfiguration>();

                _options = new PostgreSqlMessageQueueTransportOptions
                {
                    EnableRoute = true,
                    AdditionalColumnsOnMetaData = additionalColumns
                };

                _tableNameHelper = Substitute.For<ITableNameHelper>();
                _tableNameHelper.MetaDataName.Returns("meta");
                _tableNameHelper.QueueName.Returns("queue");
                _tableNameHelper.StatusName.Returns("status");

                _cache = new PostgreSqlCommandStringCache(_tableNameHelper);
            }

            public bool CacheContains(string key) => _cache.Contains(key);

            public string Build(List<string> routes) => Build(routes, out _);

            public string Build(List<string> routes, out List<NpgsqlParameter> userParams)
            {
                return ReceiveMessage.GetDeQueueCommand(_cache, _tableNameHelper, _options,
                    Configuration, routes, out userParams);
            }
        }
    }
}
