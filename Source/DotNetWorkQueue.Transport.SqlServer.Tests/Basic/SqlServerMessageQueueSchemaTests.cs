using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.Schema;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic
{
    public class SqlServerMessageQueueSchemaTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = Create();
            var tables = test.GetSchema();
            Assert.NotNull(tables);
        }

        [Fact]
        public void Create_Status()
        {
            var tableName = GetTableNameHelper();
            var options = new SqlServerMessageQueueTransportOptions {EnableStatusTable = true};
            var factory = Substitute.For<ISqlServerMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema().ConvertAll(o => (Table)o);
            Assert.Contains(tables, item => item.Name == tableName.StatusName);
        }

        [Fact]
        public void Create_Status_Extra_Columns()
        {
            var tableName = GetTableNameHelper();
            var options = new SqlServerMessageQueueTransportOptions {EnableStatusTable = true};
            options.AdditionalColumns.Add(new Column("testing", ColumnTypes.Bigint, true, null));
            var factory = Substitute.For<ISqlServerMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema().ConvertAll(o => (Table)o);
            var statusTable = tables.Find(item => item.Name == tableName.StatusName);
            Assert.Contains(statusTable.Columns.Items, item => item.Name == "testing");
        }

        [Fact]
        public void Create_Status_Extra_Constraint()
        {
            var tableName = GetTableNameHelper();
            var options = new SqlServerMessageQueueTransportOptions {EnableStatusTable = true};
            options.AdditionalColumns.Add(new Column("testing", ColumnTypes.Bigint, true, null));
            options.AdditionalConstraints.Add(new Constraint("ix_testing", ConstraintType.Index, "testing"));
            var factory = Substitute.For<ISqlServerMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema().ConvertAll(o => (Table)o);
            var statusTable = tables.Find(item => item.Name == tableName.StatusName);
            Assert.Contains(statusTable.Constraints, item => item.Name == "ix_testing");
        }

        [Fact]
        public void Create_Meta_Priority()
        {
            var tableName = GetTableNameHelper();
            var options = new SqlServerMessageQueueTransportOptions {EnablePriority = true};
            var factory = Substitute.For<ISqlServerMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema().ConvertAll(o => (Table)o);
            var statusTable = tables.Find(item => item.Name == tableName.MetaDataName);
            Assert.Contains(statusTable.Columns.Items, item => item.Name == "Priority");
        }

        [Fact]
        public void Create_Meta_Status()
        {
            var tableName = GetTableNameHelper();
            var options = new SqlServerMessageQueueTransportOptions {EnableStatus = true};
            var factory = Substitute.For<ISqlServerMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema().ConvertAll(o => (Table)o);
            var statusTable = tables.Find(item => item.Name == tableName.MetaDataName);
            Assert.Contains(statusTable.Columns.Items, item => item.Name == "Status");
        }

        [Fact]
        public void Create_Meta_DelayedProcessing()
        {
            var tableName = GetTableNameHelper();
            var options = new SqlServerMessageQueueTransportOptions {EnableDelayedProcessing = true};
            var factory = Substitute.For<ISqlServerMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema().ConvertAll(o => (Table)o);
            var statusTable = tables.Find(item => item.Name == tableName.MetaDataName);
            Assert.Contains(statusTable.Columns.Items, item => item.Name == "QueueProcessTime");
        }

        [Fact]
        public void Create_Meta_HeartBeat()
        {
            var tableName = GetTableNameHelper();
            var options = new SqlServerMessageQueueTransportOptions {EnableHeartBeat = true};
            var factory = Substitute.For<ISqlServerMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema().ConvertAll(o => (Table)o);
            var statusTable = tables.Find(item => item.Name == tableName.MetaDataName);
            Assert.Contains(statusTable.Columns.Items, item => item.Name == "HeartBeat");
        }

        [Fact]
        public void Create_Meta_MessageExpiration()
        {
            var tableName = GetTableNameHelper();
            var options = new SqlServerMessageQueueTransportOptions {EnableMessageExpiration = true};
            var factory = Substitute.For<ISqlServerMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema().ConvertAll(o => (Table)o);
            var statusTable = tables.Find(item => item.Name == tableName.MetaDataName);
            Assert.Contains(statusTable.Columns.Items, item => item.Name == "ExpirationTime");
        }

        [Fact]
        public void Create_Meta_SourceQueueID()
        {
            var tableName = GetTableNameHelper();
            var options = new SqlServerMessageQueueTransportOptions {QueueType = QueueTypes.RpcReceive};
            var factory = Substitute.For<ISqlServerMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema().ConvertAll(o => (Table)o);
            var statusTable = tables.Find(item => item.Name == tableName.MetaDataName);
            Assert.Contains(statusTable.Columns.Items, item => item.Name == "SourceQueueID");
        }

        [Fact]
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
            Assert.NotNull(tables);
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
            return Create(options, GetTableNameHelper());
        }

        private SqlServerMessageQueueSchema Create(ISqlServerMessageQueueTransportOptionsFactory options, TableNameHelper tableNameHelper)
        {
            return new SqlServerMessageQueueSchema(tableNameHelper, options);
        }

        private TableNameHelper GetTableNameHelper()
        {
            var connection = Substitute.For<IConnectionInformation>();
            connection.QueueName.Returns("test");
            return new TableNameHelper(connection);
        }
    }
}
