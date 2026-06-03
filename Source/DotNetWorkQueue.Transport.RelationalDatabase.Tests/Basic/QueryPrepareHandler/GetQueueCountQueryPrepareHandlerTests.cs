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
using System.Data;
using System.Linq;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryPrepareHandler
{
    [TestClass]
    public class GetQueueCountQueryPrepareHandlerTests
    {
        [TestMethod]
        public void Handle_With_Status_And_StatusEnabled_Binds_Int_Not_Enum()
        {
            // Regression guard for issue #155: the QueueStatusAdmin enum must be cast to its underlying
            // numeric type before being assigned to the @Status parameter. Npgsql 10.x throws
            // InvalidCastException when a raw CLR enum is bound to an integer parameter.
            var handler = new GetQueueCountQueryPrepareHandler(new FakeCommandStringCache(), CreateOptions(enableStatus: true));
            var command = CreateDbCommand();

            handler.Handle(new GetQueueCountQuery("conn", QueueStatusAdmin.Waiting), command, CommandStringTypes.GetQueueCountStatus);

            var parameters = (DataParameterCollection)command.Parameters;
            // Parameter name must match the SQL placeholder casing exactly. System.Data.SQLite binds
            // parameters case-sensitively, so "@Status" against a "@status" placeholder throws
            // "Insufficient parameters supplied to the command" (issue #155 follow-up).
            Assert.AreEqual(1, parameters.Count);
            var status = parameters.First();
            Assert.AreEqual("@status", status.ParameterName);
            Assert.AreEqual(DbType.Int32, status.DbType);
            Assert.IsInstanceOfType(status.Value, typeof(int), "@status must be bound as an int, not the QueueStatusAdmin enum");
            Assert.AreEqual((int)QueueStatusAdmin.Waiting, status.Value);
        }

        [TestMethod]
        public void Handle_With_Status_Uses_StatusFiltered_Command()
        {
            var handler = new GetQueueCountQueryPrepareHandler(new FakeCommandStringCache(), CreateOptions(enableStatus: true));
            var command = CreateDbCommand();

            handler.Handle(new GetQueueCountQuery("conn", QueueStatusAdmin.Processing), command, CommandStringTypes.GetQueueCountStatus);

            StringAssert.Contains(command.CommandText, "@status");
        }

        [TestMethod]
        public void Handle_When_StatusDisabled_Adds_No_Parameter()
        {
            // When the status table is disabled the handler ignores the filter and counts all rows.
            var handler = new GetQueueCountQueryPrepareHandler(new FakeCommandStringCache(), CreateOptions(enableStatus: false));
            var command = CreateDbCommand();

            handler.Handle(new GetQueueCountQuery("conn", QueueStatusAdmin.Waiting), command, CommandStringTypes.GetQueueCountStatus);

            Assert.IsEmpty((DataParameterCollection)command.Parameters);
        }

        private static ITransportOptionsFactory CreateOptions(bool enableStatus)
        {
            var options = Substitute.For<ITransportOptions>();
            options.EnableStatus.Returns(enableStatus);
            var factory = Substitute.For<ITransportOptionsFactory>();
            factory.Create().Returns(options);
            return factory;
        }

        private static IDbCommand CreateDbCommand()
        {
            var command = Substitute.For<IDbCommand>();
            var parameters = new DataParameterCollection();
            command.Parameters.Returns(parameters);
            command.CreateParameter().Returns(_ => Substitute.For<IDbDataParameter>());
            return command;
        }
    }
}
