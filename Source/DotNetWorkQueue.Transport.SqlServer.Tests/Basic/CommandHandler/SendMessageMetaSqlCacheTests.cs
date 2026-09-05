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
using System.Collections.Generic;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic.CommandHandler
{
    /// <summary>
    /// The meta-insert SQL is cached, so what the cache is keyed on has to account for everything
    /// that changes the text.
    /// </summary>
    /// <remarks>
    /// The failure this guards against is silent. If a flag is added to
    /// <c>AddBuiltInColumns</c> but not to <c>GetMetaSqlShape</c>, two different option shapes
    /// share one cached string and messages are written with the wrong column list.
    /// </remarks>
    [TestClass]
    public class SendMessageMetaSqlCacheTests
    {
        [TestMethod]
        public void Every_Option_Shape_Produces_Its_Own_Sql()
        {
            //the five flags the meta SQL branches on, all 32 combinations, one table name - so a
            //collision can only come from the cache key, not from the table
            var texts = new Dictionary<string, string>();

            for (var mask = 0; mask < 32; mask++)
            {
                var options = new SqlServerMessageQueueTransportOptions
                {
                    EnableDelayedProcessing = (mask & 1) != 0,
                    EnablePriority = (mask & 2) != 0,
                    EnableRoute = (mask & 4) != 0,
                    EnableStatus = (mask & 8) != 0,
                    EnableMessageExpiration = (mask & 16) != 0
                };

                var sql = BuildFor(options, "sameTable");

                Assert.IsFalse(texts.ContainsKey(sql),
                    $"option shape {mask} produced SQL already produced by shape {(texts.TryGetValue(sql, out var other) ? other : "?")}. " +
                    "The cache key is missing a flag, so two shapes would share one cached string.");
                texts[sql] = mask.ToString();
            }

            Assert.HasCount(32, texts);
        }

        [TestMethod]
        public void The_Cached_Text_Is_The_Text_That_Was_Built()
        {
            //cold then warm, same inputs: the second call comes from the cache and has to match
            var options = new SqlServerMessageQueueTransportOptions();
            var table = "roundTrip" + Guid.NewGuid().ToString("N");

            var cold = BuildFor(options, table);
            var warm = BuildFor(options, table);

            Assert.AreEqual(cold, warm);
        }

        [TestMethod]
        public void Two_Tables_Do_Not_Share_One_Cached_Text()
        {
            var options = new SqlServerMessageQueueTransportOptions();

            var first = BuildFor(options, "tableOne" + Guid.NewGuid().ToString("N"));
            var second = BuildFor(options, "tableTwo" + Guid.NewGuid().ToString("N"));

            Assert.AreNotEqual(first, second);
        }

        [TestMethod]
        public void Every_Delay_Is_Served_From_One_Cached_Statement()
        {
            //the delay was a literal in the text, so every distinct value was its own statement
            //and none of them could be cached. It is a parameter now, so one entry serves them all
            var options = new SqlServerMessageQueueTransportOptions { EnableDelayedProcessing = true };
            var table = "delayed" + Guid.NewGuid().ToString("N");

            var noDelay = BuildFor(options, table);
            var fiveSeconds = BuildFor(options, table, TimeSpan.FromSeconds(5));
            var tenSeconds = BuildFor(options, table, TimeSpan.FromSeconds(10));

            Assert.AreEqual(noDelay, fiveSeconds);
            Assert.AreEqual(fiveSeconds, tenSeconds);
            Assert.DoesNotContain("5000", fiveSeconds);
            Assert.DoesNotContain("10000", tenSeconds);
        }

        [TestMethod]
        public void Every_Expiration_Is_Served_From_One_Cached_Statement()
        {
            var options = new SqlServerMessageQueueTransportOptions { EnableMessageExpiration = true };
            var table = "expiring" + Guid.NewGuid().ToString("N");

            var none = BuildFor(options, table, expiration: TimeSpan.Zero);
            var oneMinute = BuildFor(options, table, expiration: TimeSpan.FromMinutes(1));

            Assert.AreEqual(none, oneMinute);
            Assert.DoesNotContain("60000", oneMinute);
        }

        private static string BuildFor(SqlServerMessageQueueTransportOptions options, string table,
            TimeSpan? delay = null, TimeSpan? expiration = null)
        {
            var tableNameHelper = Substitute.For<ITableNameHelper>();
            tableNameHelper.MetaDataName.Returns(table);

            var data = new AdditionalMessageData
            {
                CorrelationId = new MessageCorrelationId<Guid>(Guid.NewGuid())
            };

            using var command = new SqlCommand();
            SendMessage.BuildMetaCommand(command, tableNameHelper, Substitute.For<IHeaders>(),
                data, Substitute.For<IMessage>(), 1, options, delay,
                expiration ?? TimeSpan.Zero);

            return command.CommandText;
        }
    }
}
