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
    public class PostgreSqlJobSchemaTests
    {
        [TestMethod]
        public void GetSchema_ReturnsExactlyOneTable()
        {
            var test = Create();
            var tables = test.GetSchema();
            Assert.IsNotNull(tables);
            Assert.HasCount(1, tables);
        }

        [TestMethod]
        public void GetSchema_TableHasExpectedColumns()
        {
            var test = Create();
            var table = (Table)test.GetSchema().Single();

            Assert.HasCount(3, table.Columns.Items);

            var jobEventTime = table.Columns.Items.FirstOrDefault(c => c.Name == "JobEventTime");
            Assert.IsNotNull(jobEventTime);
            Assert.AreEqual(ColumnTypes.Bigint, jobEventTime.Type);
            Assert.IsFalse(jobEventTime.Nullable);

            var jobScheduledTime = table.Columns.Items.FirstOrDefault(c => c.Name == "JobScheduledTime");
            Assert.IsNotNull(jobScheduledTime);
            Assert.AreEqual(ColumnTypes.Bigint, jobScheduledTime.Type);
            Assert.IsFalse(jobScheduledTime.Nullable);

            var jobName = table.Columns.Items.FirstOrDefault(c => c.Name == "JobName");
            Assert.IsNotNull(jobName);
            Assert.AreEqual(ColumnTypes.Varchar, jobName.Type);
            Assert.AreEqual(255, jobName.Length);
            Assert.IsFalse(jobName.Nullable);
        }

        [TestMethod]
        public void GetSchema_TableHasPrimaryKey()
        {
            var tableNameHelper = GetTableNameHelper();
            var test = Create(tableNameHelper);
            var table = (Table)test.GetSchema().Single();

            var pk = table.Constraints.FirstOrDefault(c => c.Type == ConstraintType.PrimaryKey);
            Assert.IsNotNull(pk);
            Assert.AreEqual("PK_" + tableNameHelper.JobTableName, pk.Name);
            Assert.Contains("JobName", pk.Columns);
            Assert.IsNotNull(table.PrimaryKey);
            Assert.IsTrue(table.PrimaryKey.Unique);
        }

        [TestMethod]
        public void GetSchema_TableNameMatchesHelper()
        {
            var tableNameHelper = GetTableNameHelper();
            var test = Create(tableNameHelper);
            var table = (Table)test.GetSchema().Single();
            Assert.AreEqual(tableNameHelper.JobTableName, table.Name);
        }

        private PostgreSqlJobSchema Create()
        {
            return Create(GetTableNameHelper());
        }

        private PostgreSqlJobSchema Create(ITableNameHelper tableNameHelper)
        {
            return new PostgreSqlJobSchema(tableNameHelper);
        }

        private ITableNameHelper GetTableNameHelper()
        {
            var connection = Substitute.For<IConnectionInformation>();
            connection.QueueName.Returns("test");
            return new TableNameHelper(connection);
        }
    }
}
