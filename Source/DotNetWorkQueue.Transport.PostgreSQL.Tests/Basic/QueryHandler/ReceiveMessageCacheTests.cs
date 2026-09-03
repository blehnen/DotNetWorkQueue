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
        public void A_Cached_Statement_Still_Returns_The_User_Parameters()
        {
            //the trap. The parameters are not part of the text, so a cached return that skipped
            //them would hand back a statement referencing @p1 with nothing bound to it.
            var context = new Context(additionalColumns: true);
            context.Configuration.AddUserParameter(new NpgsqlParameter("@p1", 1));
            context.Configuration.SetUserWhereClause("AND Col = @p1");

            var cold = context.Build(null, out var coldParams);
            var warm = context.Build(null, out var warmParams);

            Assert.AreEqual(cold, warm, "the second call should come from the cache");
            Assert.IsNotNull(coldParams);
            Assert.IsNotNull(warmParams, "the cached path dropped the user parameters");
            Assert.HasCount(1, warmParams);
            Assert.AreEqual("@p1", warmParams[0].ParameterName);
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

            public string Build(List<string> routes) => Build(routes, out _);

            public string Build(List<string> routes, out List<NpgsqlParameter> userParams)
            {
                return ReceiveMessage.GetDeQueueCommand(_cache, _tableNameHelper, _options,
                    Configuration, routes, out userParams);
            }
        }
    }
}
