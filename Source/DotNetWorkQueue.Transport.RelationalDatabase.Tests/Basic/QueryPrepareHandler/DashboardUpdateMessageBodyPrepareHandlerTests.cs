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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandPrepareHandler;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryPrepareHandler
{
    public class DashboardUpdateMessageBodyPrepareHandlerTests
    {
        [Fact]
        public void Handle_Sets_CommandText()
        {
            var handler = CreateHandler();
            var command = CreateDbCommand();

            handler.Handle(new DashboardUpdateMessageBodyCommand("7", new byte[] { 1 }, new byte[] { 2 }), command,
                CommandStringTypes.DashboardUpdateMessageBody);

            Assert.NotNull(command.CommandText);
        }

        [Fact]
        public void Handle_Adds_QueueId_Body_Headers_Parameters()
        {
            var body = new byte[] { 1, 2, 3 };
            var headerBytes = new byte[] { 4, 5, 6 };
            var handler = CreateHandler();
            var command = CreateDbCommand();

            handler.Handle(new DashboardUpdateMessageBodyCommand("7", body, headerBytes), command,
                CommandStringTypes.DashboardUpdateMessageBody);

            var parameters = (DataParameterCollection)command.Parameters;
            Assert.Equal(3, parameters.Count);

            var queueIdParam = parameters.First(p => p.ParameterName == "@QueueID");
            Assert.Equal(7L, queueIdParam.Value);

            var bodyParam = parameters.First(p => p.ParameterName == "@Body");
            Assert.Equal(body, bodyParam.Value);

            var headersParam = parameters.First(p => p.ParameterName == "@Headers");
            Assert.Equal(headerBytes, headersParam.Value);
        }

        private static DashboardUpdateMessageBodyPrepareHandler CreateHandler()
        {
            return new DashboardUpdateMessageBodyPrepareHandler(new FakeCommandStringCache());
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
