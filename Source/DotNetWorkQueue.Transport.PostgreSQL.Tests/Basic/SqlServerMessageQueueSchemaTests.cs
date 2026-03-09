using System.Linq;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Schema;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic
{
    [TestClass]
    public class PostgreSqlMessageQueueSchemaTests
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
            var options = new PostgreSqlMessageQueueTransportOptions { EnableStatusTable = true };
            var factory = Substitute.For<IPostgreSqlMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema().ConvertAll(o => (Table)o);
            Assert.IsTrue(tables.Any(item => item.Name == tableName.StatusName));
        }

        [TestMethod]
        public void Create_Status_Extra_Columns()
        {
            var tableName = GetTableNameHelper();
            var options = new PostgreSqlMessageQueueTransportOptions { EnableStatusTable = true };
            options.AdditionalColumns.Add(new Column("testing", ColumnTypes.Bigint, true));
            var factory = Substitute.For<IPostgreSqlMessageQueueTransportOptionsFactory>();
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
            var options = new PostgreSqlMessageQueueTransportOptions { EnableStatusTable = true };
            options.AdditionalColumns.Add(new Column("testing", ColumnTypes.Bigint, true));
            options.AdditionalConstraints.Add(new Constraint("ix_testing", ConstraintType.Index, "testing"));
            var factory = Substitute.For<IPostgreSqlMessageQueueTransportOptionsFactory>();
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
            var options = new PostgreSqlMessageQueueTransportOptions { EnablePriority = true };
            var factory = Substitute.For<IPostgreSqlMessageQueueTransportOptionsFactory>();
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
            var options = new PostgreSqlMessageQueueTransportOptions { EnableStatus = true };
            var factory = Substitute.For<IPostgreSqlMessageQueueTransportOptionsFactory>();
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
            var options = new PostgreSqlMessageQueueTransportOptions { EnableDelayedProcessing = true };
            var factory = Substitute.For<IPostgreSqlMessageQueueTransportOptionsFactory>();
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
            var options = new PostgreSqlMessageQueueTransportOptions { EnableHeartBeat = true };
            var factory = Substitute.For<IPostgreSqlMessageQueueTransportOptionsFactory>();
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
            var options = new PostgreSqlMessageQueueTransportOptions { EnableMessageExpiration = true };
            var factory = Substitute.For<IPostgreSqlMessageQueueTransportOptionsFactory>();
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
            var options = new PostgreSqlMessageQueueTransportOptions
            {
                EnableStatus = false,
                EnableHeartBeat = false,
                EnablePriority = false
            };
            var factory = Substitute.For<IPostgreSqlMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema();
            Assert.IsNotNull(tables);
        }

        private PostgreSqlMessageQueueSchema Create()
        {
            var options = new PostgreSqlMessageQueueTransportOptions();
            var factory = Substitute.For<IPostgreSqlMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            return Create(factory);
        }

        private PostgreSqlMessageQueueSchema Create(IPostgreSqlMessageQueueTransportOptionsFactory options)
        {
            return Create(options, GetTableNameHelper());
        }

        private PostgreSqlMessageQueueSchema Create(IPostgreSqlMessageQueueTransportOptionsFactory options, ITableNameHelper tableNameHelper)
        {
            return new PostgreSqlMessageQueueSchema(tableNameHelper, options);
        }

        private ITableNameHelper GetTableNameHelper()
        {
            var connection = Substitute.For<IConnectionInformation>();
            connection.QueueName.Returns("test");
            return new TableNameHelper(connection);
        }
    }
}
