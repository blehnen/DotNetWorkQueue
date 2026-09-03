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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic.QueryHandler
{
    /// <summary>
    /// The de-queue statement is cached for routed and user-clause consumers too, not just the
    /// plain case. What the cache is keyed on has to cover everything that reaches the text.
    /// </summary>
    [TestClass]
    public class CreateDequeueStatementCacheTests
    {
        [TestMethod]
        public void Each_Route_Count_Gets_Its_Own_Statement()
        {
            //routes become @Route1..@RouteN placeholders, so the count is in the text and two
            //different counts must not share one cached statement
            var create = Create(out _);

            var one = create.GetDeQueueCommand(out _, new List<string> { "a" });
            var two = create.GetDeQueueCommand(out _, new List<string> { "a", "b" });
            var three = create.GetDeQueueCommand(out _, new List<string> { "a", "b", "c" });

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
            var create = Create(out _);

            var first = create.GetDeQueueCommand(out _, new List<string> { "a", "b" });
            var second = create.GetDeQueueCommand(out _, new List<string> { "x", "y" });

            Assert.AreEqual(first, second);
        }

        [TestMethod]
        public void No_Routes_Is_Not_Confused_With_Routes()
        {
            var create = Create(out _);

            var none = create.GetDeQueueCommand(out _);
            var one = create.GetDeQueueCommand(out _, new List<string> { "a" });

            Assert.AreNotEqual(none, one);
            Assert.DoesNotContain("@Route1", none);
        }

        [TestMethod]
        public void A_Cached_Statement_Still_Returns_The_User_Parameters()
        {
            //the trap. The parameters are not part of the text, so a cached return that skipped
            //them would hand back a statement referencing @p1 with nothing bound to it.
            var create = Create(out var configuration, additionalColumns: true);
            configuration.AddUserParameter(new SqlParameter("@p1", 1));
            configuration.SetUserWhereClause("AND Col = @p1");

            var cold = create.GetDeQueueCommand(out var coldParams);
            var warm = create.GetDeQueueCommand(out var warmParams);

            Assert.AreEqual(cold, warm, "the second call should come from the cache");
            Assert.IsNotNull(coldParams);
            Assert.IsNotNull(warmParams, "the cached path dropped the user parameters");
            Assert.HasCount(1, warmParams);
            Assert.AreEqual("@p1", warmParams[0].ParameterName);
        }

        [TestMethod]
        public void Two_User_Clauses_Do_Not_Share_One_Statement()
        {
            var create = Create(out var configuration, additionalColumns: true);
            configuration.AddUserParameter(new SqlParameter("@p1", 1));

            configuration.SetUserWhereClause("AND ColA = @p1");
            var first = create.GetDeQueueCommand(out _);

            configuration.SetUserWhereClause("AND ColB = @p1");
            var second = create.GetDeQueueCommand(out _);

            Assert.AreNotEqual(first, second);
            Assert.Contains("ColA", first);
            Assert.Contains("ColB", second);
        }

        private static CreateDequeueStatement Create(out QueueConsumerConfiguration configuration,
            bool additionalColumns = false)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            configuration = fixture.Create<QueueConsumerConfiguration>();

            var options = new SqlServerMessageQueueTransportOptions
            {
                EnableRoute = true,
                AdditionalColumnsOnMetaData = additionalColumns
            };
            var optionsFactory = Substitute.For<ISqlServerMessageQueueTransportOptionsFactory>();
            optionsFactory.Create().Returns(options);

            var tableNameHelper = Substitute.For<ITableNameHelper>();
            tableNameHelper.MetaDataName.Returns("meta");
            tableNameHelper.QueueName.Returns("queue");
            tableNameHelper.StatusName.Returns("status");

            return new CreateDequeueStatement(optionsFactory, tableNameHelper,
                new SqlServerCommandStringCache(tableNameHelper, Substitute.For<ISqlSchema>()), configuration);
        }
    }
}
