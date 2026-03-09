using System.Linq;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Schema;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    [TestClass]
    public class SqLiteMessageQueueSchemaTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = Create();
            var tables = test.GetSchema();
            Assert.IsNotNull(tables);
        }

        [TestMethod]
        public void Create_Status()
        {
            var tableName = GetTableNameHelper();
            var options = new SqLiteMessageQueueTransportOptions { EnableStatusTable = true };
            var factory = Substitute.For<ISqLiteMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema().ConvertAll(o => (Table)o);
            Assert.IsTrue(tables.Any(item => item.Name == tableName.StatusName));
        }

        [TestMethod]
        public void Create_Status_Extra_Columns()
        {
            var tableName = GetTableNameHelper();
            var options = new SqLiteMessageQueueTransportOptions { EnableStatusTable = true };
            options.AdditionalColumns.Add(new Column("testing", ColumnTypes.Integer, true, null));
            var factory = Substitute.For<ISqLiteMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema().ConvertAll(o => (Table)o);
            var statusTable = tables.Find(item => item.Name == tableName.StatusName);
            Assert.IsTrue(statusTable.Columns.Items.Any(item => item.Name == "testing"));
        }

        [TestMethod]
        public void Create_Status_Extra_Constraint()
        {
            var tableName = GetTableNameHelper();
            var options = new SqLiteMessageQueueTransportOptions { EnableStatusTable = true };
            options.AdditionalColumns.Add(new Column("testing", ColumnTypes.Integer, true, null));
            options.AdditionalConstraints.Add(new Constraint("ix_testing", ConstraintType.Index, "testing"));
            var factory = Substitute.For<ISqLiteMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema().ConvertAll(o => (Table)o);
            var statusTable = tables.Find(item => item.Name == tableName.StatusName);
            Assert.IsTrue(statusTable.Constraints.Any(item => item.Name == "ix_testing"));
        }

        [TestMethod]
        public void Create_Meta_Priority()
        {
            var tableName = GetTableNameHelper();
            var options = new SqLiteMessageQueueTransportOptions { EnablePriority = true };
            var factory = Substitute.For<ISqLiteMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema().ConvertAll(o => (Table)o);
            var statusTable = tables.Find(item => item.Name == tableName.MetaDataName);
            Assert.IsTrue(statusTable.Columns.Items.Any(item => item.Name == "Priority"));
        }

        [TestMethod]
        public void Create_Meta_Status()
        {
            var tableName = GetTableNameHelper();
            var options = new SqLiteMessageQueueTransportOptions { EnableStatus = true };
            var factory = Substitute.For<ISqLiteMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema().ConvertAll(o => (Table)o);
            var statusTable = tables.Find(item => item.Name == tableName.MetaDataName);
            Assert.IsTrue(statusTable.Columns.Items.Any(item => item.Name == "Status"));
        }

        [TestMethod]
        public void Create_Meta_DelayedProcessing()
        {
            var tableName = GetTableNameHelper();
            var options = new SqLiteMessageQueueTransportOptions { EnableDelayedProcessing = true };
            var factory = Substitute.For<ISqLiteMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema().ConvertAll(o => (Table)o);
            var statusTable = tables.Find(item => item.Name == tableName.MetaDataName);
            Assert.IsTrue(statusTable.Columns.Items.Any(item => item.Name == "QueueProcessTime"));
        }

        [TestMethod]
        public void Create_Meta_HeartBeat()
        {
            var tableName = GetTableNameHelper();
            var options = new SqLiteMessageQueueTransportOptions { EnableHeartBeat = true };
            var factory = Substitute.For<ISqLiteMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema().ConvertAll(o => (Table)o);
            var statusTable = tables.Find(item => item.Name == tableName.MetaDataName);
            Assert.IsTrue(statusTable.Columns.Items.Any(item => item.Name == "HeartBeat"));
        }

        [TestMethod]
        public void Create_Meta_MessageExpiration()
        {
            var tableName = GetTableNameHelper();
            var options = new SqLiteMessageQueueTransportOptions { EnableMessageExpiration = true };
            var factory = Substitute.For<ISqLiteMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema().ConvertAll(o => (Table)o);
            var statusTable = tables.Find(item => item.Name == tableName.MetaDataName);
            Assert.IsTrue(statusTable.Columns.Items.Any(item => item.Name == "ExpirationTime"));
        }

        [TestMethod]
        public void Create_FIFO()
        {
            var tableName = GetTableNameHelper();
            var options = new SqLiteMessageQueueTransportOptions
            {
                EnableStatus = false,
                EnableHeartBeat = false,
                EnablePriority = false
            };
            var factory = Substitute.For<ISqLiteMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema();
            Assert.IsNotNull(tables);
        }

        private SqLiteMessageQueueSchema Create()
        {
            var options = new SqLiteMessageQueueTransportOptions();
            var factory = Substitute.For<ISqLiteMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            return Create(factory);
        }

        private SqLiteMessageQueueSchema Create(ISqLiteMessageQueueTransportOptionsFactory options)
        {
            return Create(options, GetTableNameHelper());
        }

        private SqLiteMessageQueueSchema Create(ISqLiteMessageQueueTransportOptionsFactory options, TableNameHelper tableNameHelper)
        {
            return new SqLiteMessageQueueSchema(tableNameHelper, options);
        }

        private TableNameHelper GetTableNameHelper()
        {
            var connection = Substitute.For<IConnectionInformation>();
            connection.QueueName.Returns("test");
            return new TableNameHelper(connection);
        }
    }
}
