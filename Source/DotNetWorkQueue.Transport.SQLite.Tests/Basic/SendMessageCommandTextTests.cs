using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SQLite;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Basic.CommandHandler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    /// <summary>
    /// The send path builds its meta data and status inserts with a <c>StringBuilder</c> on every
    /// send. A pooled connection files its compiled statements by command text, so reuse only
    /// happens while that text is identical from one send to the next. These pin that.
    /// </summary>
    [TestClass]
    public class SendMessageCommandTextTests
    {
        private static ITableNameHelper TableNames() =>
            new TableNameHelper(new SqliteConnectionInformation(
                new QueueConnection("q", @"Data Source=c:\test\t.db3;Version=3;"), new DbDataSource()));

        private static SqLiteMessageQueueTransportOptions Options(bool userColumnsOnMetaData = false) =>
            new SqLiteMessageQueueTransportOptions { AdditionalColumnsOnMetaData = userColumnsOnMetaData };

        [TestMethod]
        public void TheMetaDataInsertIsTheSameTextForEverySend()
        {
            //the property the command reuse depends on
            var tables = TableNames();
            var options = Options();

            var first = SendMessage.BuildMetaCommandText(tables, new AdditionalMessageData(), options);
            var second = SendMessage.BuildMetaCommandText(tables, new AdditionalMessageData(), options);

            Assert.AreEqual(first, second);
        }

        [TestMethod]
        public void TheStatusInsertIsTheSameTextForEverySend()
        {
            var tables = TableNames();
            var options = Options();

            var first = SendMessage.BuildStatusCommandText(tables, new AdditionalMessageData(), options);
            var second = SendMessage.BuildStatusCommandText(tables, new AdditionalMessageData(), options);

            Assert.AreEqual(first, second);
        }

        [TestMethod]
        public void UserColumnsOnMetaData_ChangeTheTextWithTheMessage()
        {
            //A documented limit rather than a defect: with user columns on the meta data the insert
            //names them, so a caller sending different columns per message gets a different
            //statement per message and no reuse. The cache is bounded for exactly this reason.
            var tables = TableNames();
            var options = Options(userColumnsOnMetaData: true);

            var withOrder = new AdditionalMessageData();
            withOrder.AdditionalMetaData.Add(new AdditionalMetaData<int>("OrderID", 1));

            var withCustomer = new AdditionalMessageData();
            withCustomer.AdditionalMetaData.Add(new AdditionalMetaData<int>("CustomerID", 1));

            Assert.AreNotEqual(
                SendMessage.BuildMetaCommandText(tables, withOrder, options),
                SendMessage.BuildMetaCommandText(tables, withCustomer, options));
        }

        [TestMethod]
        public void TheSameUserColumns_StillGiveTheSameText()
        {
            //the value differs, only the column names are in the text
            var tables = TableNames();
            var options = Options(userColumnsOnMetaData: true);

            var first = new AdditionalMessageData();
            first.AdditionalMetaData.Add(new AdditionalMetaData<int>("OrderID", 1));

            var second = new AdditionalMessageData();
            second.AdditionalMetaData.Add(new AdditionalMetaData<int>("OrderID", 99));

            Assert.AreEqual(
                SendMessage.BuildMetaCommandText(tables, first, options),
                SendMessage.BuildMetaCommandText(tables, second, options));
        }

        [TestMethod]
        public void TheMetaDataAndStatusInsertsAreDifferentStatements()
        {
            //they are cached separately; if they collided one would be served for the other
            var tables = TableNames();
            var options = Options();

            Assert.AreNotEqual(
                SendMessage.BuildMetaCommandText(tables, new AdditionalMessageData(), options),
                SendMessage.BuildStatusCommandText(tables, new AdditionalMessageData(), options));
        }
    }
}
