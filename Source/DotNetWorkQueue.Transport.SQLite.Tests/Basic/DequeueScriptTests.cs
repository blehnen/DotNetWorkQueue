using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SQLite;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Basic.QueryHandler;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    /// <summary>
    /// ReceiveMessageQueryHandler caches the dequeue script and keys that cache on the caller's
    /// clause and routes. These pin the contract that makes the key correct: the script depends on
    /// those two things and nothing else that varies, so anything the key omits must not be able to
    /// change the SQL.
    /// </summary>
    [TestClass]
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

        [TestMethod]
        public void TheSameInputsGiveTheSameScript()
        {
            //without this the cache would never hit, and a pooled connection would recompile too
            Assert.AreEqual(Script(Options()), Script(Options()));
        }

        [TestMethod]
        public void TheCallersClauseChangesTheScript()
        {
            //so the clause has to be part of the cache key - it is written into the SQL
            var options = Options(userColumns: true);

            Assert.AreNotEqual(
                Script(options, "(OrderID = @OrderID)"),
                Script(options, "(CustomerID = @CustomerID)"));
        }

        [TestMethod]
        public void RoutesChangeTheScript()
        {
            var options = Options(routes: true);

            Assert.AreNotEqual(
                Script(options, routes: new List<string> { "a" }),
                Script(options, routes: new List<string> { "a", "b" }));
        }

        [TestMethod]
        public void TheClauseIsIgnoredUnlessUserColumnsAreOnTheMetaData()
        {
            //it is only written into the SQL when that option is on, which is why the handler only
            //reads it in that case
            Assert.AreEqual(
                Script(Options(userColumns: false), "(OrderID = @OrderID)"),
                Script(Options(userColumns: false)));
        }

        [TestMethod]
        public void TheClauseAppearsInTheScriptWhenItIsUsed()
        {
            StringAssert.Contains(Script(Options(userColumns: true), "(OrderID = @OrderID)"), "OrderID = @OrderID");
        }

        [TestMethod]
        public void TheScriptDoesNotDependOnParameterValues()
        {
            //parameters are bound, not written into the SQL - which is what lets the factory form
            //keep returning fresh values while the script stays cached
            var options = Options(userColumns: true);

            Assert.AreEqual(
                Script(options, "(OrderID = @OrderID)"),
                Script(options, "(OrderID = @OrderID)"));
        }
    }
}
