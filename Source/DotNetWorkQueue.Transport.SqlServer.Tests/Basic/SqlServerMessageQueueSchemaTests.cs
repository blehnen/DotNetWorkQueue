using System.Linq;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.Schema;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic
{
    [TestClass]
    public class SqlServerMessageQueueSchemaTests
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
            var options = new SqlServerMessageQueueTransportOptions { EnableStatusTable = true };
            var factory = Substitute.For<ISqlServerMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema().ConvertAll(o => (Table)o);
            Assert.IsTrue(tables.Any(item => item.Name == tableName.StatusName));
        }

        [TestMethod]
        public void Create_Status_Extra_Columns()
        {
            var tableName = GetTableNameHelper();
            var options = new SqlServerMessageQueueTransportOptions { EnableStatusTable = true };
            options.AdditionalColumns.Add(new Column("testing", ColumnTypes.Bigint, true, null));
            var factory = Substitute.For<ISqlServerMessageQueueTransportOptionsFactory>();
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
            var options = new SqlServerMessageQueueTransportOptions { EnableStatusTable = true };
            options.AdditionalColumns.Add(new Column("testing", ColumnTypes.Bigint, true, null));
            options.AdditionalConstraints.Add(new Constraint("ix_testing", ConstraintType.Index, "testing"));
            var factory = Substitute.For<ISqlServerMessageQueueTransportOptionsFactory>();
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
            var options = new SqlServerMessageQueueTransportOptions { EnablePriority = true };
            var factory = Substitute.For<ISqlServerMessageQueueTransportOptionsFactory>();
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
            var options = new SqlServerMessageQueueTransportOptions { EnableStatus = true };
            var factory = Substitute.For<ISqlServerMessageQueueTransportOptionsFactory>();
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
            var options = new SqlServerMessageQueueTransportOptions { EnableDelayedProcessing = true };
            var factory = Substitute.For<ISqlServerMessageQueueTransportOptionsFactory>();
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
            var options = new SqlServerMessageQueueTransportOptions { EnableHeartBeat = true };
            var factory = Substitute.For<ISqlServerMessageQueueTransportOptionsFactory>();
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
            var options = new SqlServerMessageQueueTransportOptions { EnableMessageExpiration = true };
            var factory = Substitute.For<ISqlServerMessageQueueTransportOptionsFactory>();
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
            var options = new SqlServerMessageQueueTransportOptions
            {
                EnableStatus = false,
                EnableHeartBeat = false,
                EnablePriority = false
            };
            var factory = Substitute.For<ISqlServerMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema();
            Assert.IsNotNull(tables);
        }

        private SqlServerMessageQueueSchema Create()
        {
            var options = new SqlServerMessageQueueTransportOptions();
            var factory = Substitute.For<ISqlServerMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            return Create(factory);
        }

        private SqlServerMessageQueueSchema Create(ISqlServerMessageQueueTransportOptionsFactory options)
        {
            var connection = Substitute.For<IConnectionInformation>();
            connection.QueueName.Returns("test");
            return Create(options, GetTableNameHelper(connection), connection);
        }

        private SqlServerMessageQueueSchema Create(ISqlServerMessageQueueTransportOptionsFactory options, TableNameHelper tableNameHelper)
        {
            var connection = Substitute.For<IConnectionInformation>();
            connection.QueueName.Returns("test");
            return Create(options, tableNameHelper, connection);
        }

        private SqlServerMessageQueueSchema Create(ISqlServerMessageQueueTransportOptionsFactory options, TableNameHelper tableNameHelper, IConnectionInformation connectionInformation)
        {
            return new SqlServerMessageQueueSchema(tableNameHelper, options, new SqlSchema(connectionInformation));
        }

        private TableNameHelper GetTableNameHelper()
        {
            var connection = Substitute.For<IConnectionInformation>();
            connection.QueueName.Returns("test");
            return new TableNameHelper(connection);
        }

        private TableNameHelper GetTableNameHelper(IConnectionInformation connection)
        {
            return new TableNameHelper(connection);
        }
    }
}
