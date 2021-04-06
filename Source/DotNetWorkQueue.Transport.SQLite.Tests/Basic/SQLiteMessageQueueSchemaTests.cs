using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Schema;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    public class SqLiteMessageQueueSchemaTests
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
            var options = new SqLiteMessageQueueTransportOptions { EnableStatusTable = true};
            var factory = Substitute.For<ISqLiteMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema().ConvertAll(o => (Table)o);
            Assert.Contains(tables, item => item.Name == tableName.StatusName);
        }

        [Fact]
        public void Create_Status_Extra_Columns()
        {
            var tableName = GetTableNameHelper();
            var options = new SqLiteMessageQueueTransportOptions { EnableStatusTable = true};
            options.AdditionalColumns.Add(new Column("testing", ColumnTypes.Integer, true, null));
            var factory = Substitute.For<ISqLiteMessageQueueTransportOptionsFactory>();
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
            var options = new SqLiteMessageQueueTransportOptions { EnableStatusTable = true};
            options.AdditionalColumns.Add(new Column("testing", ColumnTypes.Integer, true, null));
            options.AdditionalConstraints.Add(new Constraint("ix_testing", ConstraintType.Index, "testing"));
            var factory = Substitute.For<ISqLiteMessageQueueTransportOptionsFactory>();
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
            var options = new SqLiteMessageQueueTransportOptions { EnablePriority = true};
            var factory = Substitute.For<ISqLiteMessageQueueTransportOptionsFactory>();
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
            var options = new SqLiteMessageQueueTransportOptions { EnableStatus = true};
            var factory = Substitute.For<ISqLiteMessageQueueTransportOptionsFactory>();
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
            var options = new SqLiteMessageQueueTransportOptions { EnableDelayedProcessing = true};
            var factory = Substitute.For<ISqLiteMessageQueueTransportOptionsFactory>();
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
            var options = new SqLiteMessageQueueTransportOptions { EnableHeartBeat = true};
            var factory = Substitute.For<ISqLiteMessageQueueTransportOptionsFactory>();
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
            var options = new SqLiteMessageQueueTransportOptions { EnableMessageExpiration = true};
            var factory = Substitute.For<ISqLiteMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema().ConvertAll(o => (Table)o);
            var statusTable = tables.Find(item => item.Name == tableName.MetaDataName);
            Assert.Contains(statusTable.Columns.Items, item => item.Name == "ExpirationTime");
        }

        [Fact]
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
            Assert.NotNull(tables);
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
