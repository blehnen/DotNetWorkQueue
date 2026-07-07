// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System.Linq;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    [TestClass]
    public class SqliteJobSchemaTests
    {
        private const string JobTableName = "TestJobTable";

        [TestMethod]
        public void GetSchema_ReturnsExactlyOneTable()
        {
            var schema = CreateSchema();

            var tables = schema.GetSchema();

            Assert.IsNotNull(tables);
            Assert.HasCount(1, tables);
        }

        [TestMethod]
        public void GetSchema_TableHasExpectedColumns()
        {
            var schema = CreateSchema();

            var table = (Table)schema.GetSchema().Single();

            Assert.HasCount(3, table.Columns.Items);

            var jobEventTime = table.Columns.Items.Single(c => c.Name == "JobEventTime");
            Assert.AreEqual(ColumnTypes.Text, jobEventTime.Type);
            Assert.AreEqual(35, jobEventTime.Length);
            Assert.IsFalse(jobEventTime.Nullable);

            var jobScheduledTime = table.Columns.Items.Single(c => c.Name == "JobScheduledTime");
            Assert.AreEqual(ColumnTypes.Text, jobScheduledTime.Type);
            Assert.AreEqual(35, jobScheduledTime.Length);
            Assert.IsFalse(jobScheduledTime.Nullable);

            var jobName = table.Columns.Items.Single(c => c.Name == "JobName");
            Assert.AreEqual(ColumnTypes.Text, jobName.Type);
            Assert.AreEqual(255, jobName.Length);
            Assert.IsFalse(jobName.Nullable);
        }

        [TestMethod]
        public void GetSchema_TableHasPrimaryKey()
        {
            var schema = CreateSchema();

            var table = (Table)schema.GetSchema().Single();

            Assert.IsNotNull(table.PrimaryKey);
            Assert.AreEqual(ConstraintType.PrimaryKey, table.PrimaryKey.Type);
            Assert.AreEqual("PK_" + JobTableName, table.PrimaryKey.Name);
            Assert.IsTrue(table.PrimaryKey.Unique);
            CollectionAssert.Contains(table.PrimaryKey.Columns, "JobName");
        }

        [TestMethod]
        public void GetSchema_TableNameMatchesHelper()
        {
            var schema = CreateSchema();

            var table = (Table)schema.GetSchema().Single();

            Assert.AreEqual(JobTableName, table.Name);
        }

        private static SqliteJobSchema CreateSchema()
        {
            var tableNameHelper = Substitute.For<ITableNameHelper>();
            tableNameHelper.JobTableName.Returns(JobTableName);
            return new SqliteJobSchema(tableNameHelper);
        }
    }
}
