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
    public class SqlServerJobSchemaTests
    {
        [TestMethod]
        public void GetSchema_ReturnsExactlyOneTable()
        {
            var fixture = CreateFixture();

            var tables = fixture.Schema.GetSchema();

            Assert.IsNotNull(tables);
            Assert.HasCount(1, tables);
        }

        [TestMethod]
        public void GetSchema_TableHasExpectedColumns()
        {
            var fixture = CreateFixture();

            var table = (Table)fixture.Schema.GetSchema().Single();

            Assert.HasCount(3, table.Columns.Items);

            var jobEventTime = table.Columns.Items.SingleOrDefault(c => c.Name == "JobEventTime");
            Assert.IsNotNull(jobEventTime);
            Assert.AreEqual(ColumnTypes.Datetimeoffset, jobEventTime.Type);
            Assert.IsFalse(jobEventTime.Nullable);

            var jobScheduledTime = table.Columns.Items.SingleOrDefault(c => c.Name == "JobScheduledTime");
            Assert.IsNotNull(jobScheduledTime);
            Assert.AreEqual(ColumnTypes.Datetimeoffset, jobScheduledTime.Type);
            Assert.IsFalse(jobScheduledTime.Nullable);

            var jobName = table.Columns.Items.SingleOrDefault(c => c.Name == "JobName");
            Assert.IsNotNull(jobName);
            Assert.AreEqual(ColumnTypes.Varchar, jobName.Type);
            Assert.AreEqual(255, jobName.Length);
            Assert.IsFalse(jobName.Nullable);
        }

        [TestMethod]
        public void GetSchema_TableHasPrimaryKey()
        {
            var fixture = CreateFixture();

            var table = (Table)fixture.Schema.GetSchema().Single();

            Assert.IsNotNull(table.PrimaryKey);
            Assert.AreEqual("PK_" + fixture.TableNameHelper.JobTableName, table.PrimaryKey.Name);
            Assert.AreEqual(ConstraintType.PrimaryKey, table.PrimaryKey.Type);
            Assert.IsTrue(table.PrimaryKey.Clustered);
            Assert.IsTrue(table.PrimaryKey.Unique);
            Assert.HasCount(1, table.PrimaryKey.Columns);
            Assert.AreEqual("JobName", table.PrimaryKey.Columns.Single());
        }

        [TestMethod]
        public void GetSchema_TableNameMatchesHelper()
        {
            var fixture = CreateFixture();

            var table = (Table)fixture.Schema.GetSchema().Single();

            Assert.AreEqual(fixture.TableNameHelper.JobTableName, table.Name);
        }

        [TestMethod]
        public void GetSchema_TableOwnerMatchesSchema()
        {
            var fixture = CreateFixture("custom_schema");

            var table = (Table)fixture.Schema.GetSchema().Single();

            Assert.AreEqual("custom_schema", table.Owner);
        }

        private TestFixture CreateFixture(string schemaName = "dbo")
        {
            var connection = Substitute.For<IConnectionInformation>();
            connection.QueueName.Returns("testJobQueue");

            var tableNameHelper = new TableNameHelper(connection);

            var sqlSchema = Substitute.For<ISqlSchema>();
            sqlSchema.Schema.Returns(schemaName);

            var schema = new SqlServerJobSchema(tableNameHelper, sqlSchema);

            return new TestFixture
            {
                Schema = schema,
                TableNameHelper = tableNameHelper,
                SqlSchema = sqlSchema
            };
        }

        private class TestFixture
        {
            public SqlServerJobSchema Schema { get; set; }
            public TableNameHelper TableNameHelper { get; set; }
            public ISqlSchema SqlSchema { get; set; }
        }
    }
}
