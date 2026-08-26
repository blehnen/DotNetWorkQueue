using System;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Basic.QueryHandler;
using FluentAssertions;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    /// <summary>
    /// Covers the temp table a dequeue stages its candidate row in. It is named after the queue
    /// rather than generated per call, because a temp table lives until its connection closes and
    /// connections are pooled and held open for the life of the queue.
    /// </summary>
    public class ReceiveMessageTempTableTests
    {
        private static CommandString Build(string metaTableName = "qMetaData")
        {
            return ReceiveMessage.GetDeQueueCommand(metaTableName, "q", "qStatus",
                new SqLiteMessageQueueTransportOptions(), null, null);
        }

        [Fact]
        public void TheTempTableIsOnlyCreatedWhenMissing()
        {
            //a plain CREATE would be new DDL on every dequeue, and every table would stay resident
            Assert.Contains("CREATE TEMP TABLE IF NOT EXISTS", Build().CommandText);
        }

        [Fact]
        public void TheTempTableIsClearedBeforeUse()
        {
            //a dequeue that committed leaves its row behind for the next one
            Assert.Contains("DELETE FROM", Build().CommandText);
        }

        [Fact]
        public void TheSameQueueAlwaysGetsTheSameTempTable()
        {
            //the property the fix depends on: without it every dequeue creates another table
            Assert.Equal(Build().CommandText, Build().CommandText);
        }

        [Fact]
        public void DifferentQueuesGetDifferentTempTables()
        {
            var first = TempTableNameOf(Build("oneMetaData"));
            var second = TempTableNameOf(Build("twoMetaData"));

            Assert.NotEqual(first, second);
        }

        [Fact]
        public void RepeatedDequeuesDoNotAccumulateTempTables()
        {
            //Runs the staging statements the generator emits, twice, on one connection - the thing
            //the receive path does per message. Before the fix this left one table per dequeue.
            var staging = StagingStatements(Build());

            var dir = Path.Combine(Path.GetTempPath(), "dnwq-temp-table-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                using var connection = new SQLiteConnection($"Data Source={Path.Combine(dir, "t.db")};Version=3;");
                connection.Open();

                for (var i = 0; i < 10; i++)
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = staging;
                    command.ExecuteNonQuery();
                }

                using var count = connection.CreateCommand();
                count.CommandText = "select count(*) from sqlite_temp_master where type='table'";

                Convert.ToInt64(count.ExecuteScalar()).Should().Be(1L,
                    "ten dequeues on one connection should share a single temp table");
            }
            finally
            {
                SQLiteConnection.ClearAllPools();
                try { Directory.Delete(dir, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }

        /// <summary>
        /// The CREATE and DELETE the script opens with. Taking them by position couples this test to
        /// the order the generator emits them in, which is the point - if that changes, this should
        /// fail rather than quietly stop testing anything.
        /// </summary>
        private static string StagingStatements(CommandString commandString)
        {
            var lines = commandString.CommandText.Split('\n').Take(2).ToArray();

            Assert.True(lines[0].Contains("CREATE TEMP TABLE", StringComparison.OrdinalIgnoreCase), $"expected the script to open with the temp table create, found '{lines[0]}'");
            Assert.True(lines[1].Contains("DELETE FROM", StringComparison.OrdinalIgnoreCase), $"expected the clear to follow the create, found '{lines[1]}'");

            return string.Join(Environment.NewLine, lines);
        }

        private static string TempTableNameOf(CommandString commandString)
        {
            var line = commandString.CommandText.Split('\n')[0];
            var start = line.IndexOf("EXISTS ", StringComparison.OrdinalIgnoreCase) + "EXISTS ".Length;
            return line.Substring(start, line.IndexOf('(', start) - start);
        }
    }
}
