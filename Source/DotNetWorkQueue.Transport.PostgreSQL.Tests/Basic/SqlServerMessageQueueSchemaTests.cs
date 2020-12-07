using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Schema;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic
{
    public class PostgreSqlMessageQueueSchemaTests
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
            var options = new PostgreSqlMessageQueueTransportOptions {EnableStatusTable = true};
            var factory = Substitute.For<IPostgreSqlMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema().ConvertAll(o => (Table)o);
            Assert.Contains(tables, item => item.Name == tableName.StatusName);
        }

        [Fact]
        public void Create_Status_Extra_Columns()
        {
            var tableName = GetTableNameHelper();
            var options = new PostgreSqlMessageQueueTransportOptions {EnableStatusTable = true};
            options.AdditionalColumns.Add(new Column("testing", ColumnTypes.Bigint, true));
            var factory = Substitute.For<IPostgreSqlMessageQueueTransportOptionsFactory>();
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
            var options = new PostgreSqlMessageQueueTransportOptions {EnableStatusTable = true};
            options.AdditionalColumns.Add(new Column("testing", ColumnTypes.Bigint, true));
            options.AdditionalConstraints.Add(new Constraint("ix_testing", ConstraintType.Index, "testing"));
            var factory = Substitute.For<IPostgreSqlMessageQueueTransportOptionsFactory>();
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
            var options = new PostgreSqlMessageQueueTransportOptions {EnablePriority = true};
            var factory = Substitute.For<IPostgreSqlMessageQueueTransportOptionsFactory>();
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
            var options = new PostgreSqlMessageQueueTransportOptions {EnableStatus = true};
            var factory = Substitute.For<IPostgreSqlMessageQueueTransportOptionsFactory>();
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
            var options = new PostgreSqlMessageQueueTransportOptions {EnableDelayedProcessing = true};
            var factory = Substitute.For<IPostgreSqlMessageQueueTransportOptionsFactory>();
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
            var options = new PostgreSqlMessageQueueTransportOptions {EnableHeartBeat = true};
            var factory = Substitute.For<IPostgreSqlMessageQueueTransportOptionsFactory>();
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
            var options = new PostgreSqlMessageQueueTransportOptions {EnableMessageExpiration = true};
            var factory = Substitute.For<IPostgreSqlMessageQueueTransportOptionsFactory>();
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
            Assert.NotNull(tables);
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
