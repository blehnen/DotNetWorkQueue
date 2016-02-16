// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
            var tables = test.GetSchema();
            Assert.True(tables.Any(item => item.Name == tableName.StatusName));
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
            var tables = test.GetSchema();
            var statusTable = tables.Find(item => item.Name == tableName.StatusName);
            Assert.True(statusTable.Columns.Items.Any(item => item.Name == "testing"));
        }

        [Fact]
        public void Create_Status_Extra_Constraint()
        {
            var tableName = GetTableNameHelper();
            var options = new SqLiteMessageQueueTransportOptions { EnableStatusTable = true};
            options.AdditionalColumns.Add(new Column("testing", ColumnTypes.Integer, true, null));
            options.AdditionalConstraints.Add(new Constraint("ix_testing", ContraintType.Index, "testing"));
            var factory = Substitute.For<ISqLiteMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema();
            var statusTable = tables.Find(item => item.Name == tableName.StatusName);
            Assert.True(statusTable.Constraints.Any(item => item.Name == "ix_testing"));
        }

        [Fact]
        public void Create_Meta_Priority()
        {
            var tableName = GetTableNameHelper();
            var options = new SqLiteMessageQueueTransportOptions { EnablePriority = true};
            var factory = Substitute.For<ISqLiteMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema();
            var statusTable = tables.Find(item => item.Name == tableName.MetaDataName);
            Assert.True(statusTable.Columns.Items.Any(item => item.Name == "Priority"));
        }

        [Fact]
        public void Create_Meta_Status()
        {
            var tableName = GetTableNameHelper();
            var options = new SqLiteMessageQueueTransportOptions { EnableStatus = true};
            var factory = Substitute.For<ISqLiteMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema();
            var statusTable = tables.Find(item => item.Name == tableName.MetaDataName);
            Assert.True(statusTable.Columns.Items.Any(item => item.Name == "Status"));
        }

        [Fact]
        public void Create_Meta_DelayedProcessing()
        {
            var tableName = GetTableNameHelper();
            var options = new SqLiteMessageQueueTransportOptions { EnableDelayedProcessing = true};
            var factory = Substitute.For<ISqLiteMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema();
            var statusTable = tables.Find(item => item.Name == tableName.MetaDataName);
            Assert.True(statusTable.Columns.Items.Any(item => item.Name == "QueueProcessTime"));
        }

        [Fact]
        public void Create_Meta_HeartBeat()
        {
            var tableName = GetTableNameHelper();
            var options = new SqLiteMessageQueueTransportOptions { EnableHeartBeat = true};
            var factory = Substitute.For<ISqLiteMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema();
            var statusTable = tables.Find(item => item.Name == tableName.MetaDataName);
            Assert.True(statusTable.Columns.Items.Any(item => item.Name == "HeartBeat"));
        }

        [Fact]
        public void Create_Meta_MessageExpiration()
        {
            var tableName = GetTableNameHelper();
            var options = new SqLiteMessageQueueTransportOptions { EnableMessageExpiration = true};
            var factory = Substitute.For<ISqLiteMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema();
            var statusTable = tables.Find(item => item.Name == tableName.MetaDataName);
            Assert.True(statusTable.Columns.Items.Any(item => item.Name == "ExpirationTime"));
        }

        [Fact]
        public void Create_Meta_SourceQueueID()
        {
            var tableName = GetTableNameHelper();
            var options = new SqLiteMessageQueueTransportOptions { QueueType = QueueTypes.RpcReceive};
            var factory = Substitute.For<ISqLiteMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            var test = Create(factory, tableName);
            var tables = test.GetSchema();
            var statusTable = tables.Find(item => item.Name == tableName.MetaDataName);
            Assert.True(statusTable.Columns.Items.Any(item => item.Name == "SourceQueueID"));
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
