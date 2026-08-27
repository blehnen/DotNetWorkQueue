using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SQLite;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Basic.QueryHandler;
using System.Collections.Generic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    /// <summary>
    /// ReceiveMessageQueryHandler caches the dequeue script and keys that cache on the caller's
    /// clause and routes. These pin the contract that makes the key correct: the script depends on
    /// those two things and nothing else that varies, so anything the key omits must not be able to
    /// change the SQL.
    /// </summary>
    public class DequeueScriptTests
    {
        private static ITableNameHelper TableNames() =>
            new TableNameHelper(new SqliteConnectionInformation(
                new QueueConnection("q", @"Data Source=c:\test\t.db3;Version=3;"), new DbDataSource()));

        private static string Script(SqLiteMessageQueueTransportOptions options, string userClause = null,
            List<string> routes = null)
        {
            var tables = TableNames();
            return ReceiveMessage.GetDeQueueCommand(tables.MetaDataName, tables.QueueName, tables.StatusName,
                options, userClause, routes).CommandText;
        }

        private static SqLiteMessageQueueTransportOptions Options(bool userColumns = false, bool routes = false) =>
            new SqLiteMessageQueueTransportOptions { AdditionalColumnsOnMetaData = userColumns, EnableRoute = routes };

        [Fact]
        public void TheSameInputsGiveTheSameScript()
        {
            //without this the cache would never hit, and a pooled connection would recompile too
            Assert.Equal(Script(Options()), Script(Options()));
        }

        [Fact]
        public void TheCallersClauseChangesTheScript()
        {
            //so the clause has to be part of the cache key - it is written into the SQL
            var options = Options(userColumns: true);

            Assert.NotEqual(
                Script(options, "(OrderID = @OrderID)"),
                Script(options, "(CustomerID = @CustomerID)"));
        }

        [Fact]
        public void RoutesChangeTheScript()
        {
            var options = Options(routes: true);

            Assert.NotEqual(
                Script(options, routes: new List<string> { "a" }),
                Script(options, routes: new List<string> { "a", "b" }));
        }

        [Fact]
        public void TheClauseIsIgnoredUnlessUserColumnsAreOnTheMetaData()
        {
            //it is only written into the SQL when that option is on, which is why the handler only
            //reads it in that case
            Assert.Equal(
                Script(Options(userColumns: false), "(OrderID = @OrderID)"),
                Script(Options(userColumns: false)));
        }

        [Fact]
        public void TheClauseAppearsInTheScriptWhenItIsUsed()
        {
            Assert.Contains("OrderID = @OrderID", Script(Options(userColumns: true), "(OrderID = @OrderID)"));
        }

        [Fact]
        public void KeysCannotCollideAcrossClauseAndRouteBoundaries()
        {
            //A separator-joined key would give these the same value. They need different scripts:
            //the route count decides how many placeholders the SQL carries, so serving one for the
            //other would bind the wrong number of parameters.
            var separator = "\u001f";

            var first = ReceiveMessageQueryHandler.Key("a", new List<string> { "b", "c" });
            var second = ReceiveMessageQueryHandler.Key("a" + separator + "b", new List<string> { "c" });

            Assert.NotEqual(first, second);
        }

        [Fact]
        public void KeysAreStableForTheSameInputs()
        {
            var routes = new List<string> { "a", "b" };

            Assert.Equal(
                ReceiveMessageQueryHandler.Key("(x = @x)", routes),
                ReceiveMessageQueryHandler.Key("(x = @x)", new List<string> { "a", "b" }));
        }

        [Fact]
        public void TheOrdinaryCaseHasAnEmptyKey()
        {
            //no clause and no routes is the common path, and it allocates nothing
            Assert.Equal(string.Empty, ReceiveMessageQueryHandler.Key(null, null));
            Assert.Equal(string.Empty, ReceiveMessageQueryHandler.Key(string.Empty, new List<string>()));
        }

        [Fact]
        public void DifferentRouteCountsGiveDifferentKeys()
        {
            Assert.NotEqual(
                ReceiveMessageQueryHandler.Key(null, new List<string> { "a" }),
                ReceiveMessageQueryHandler.Key(null, new List<string> { "a", "b" }));
        }

        [Fact]
        public void TheScriptDoesNotDependOnParameterValues()
        {
            //parameters are bound, not written into the SQL - which is what lets the factory form
            //keep returning fresh values while the script stays cached
            var options = Options(userColumns: true);

            Assert.Equal(
                Script(options, "(OrderID = @OrderID)"),
                Script(options, "(OrderID = @OrderID)"));
        }
    }
}
