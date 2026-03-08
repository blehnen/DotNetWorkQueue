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
    public class DashboardRequeueErrorMessagePrepareHandlerTests
    {
        [Fact]
        public void Handle_Sets_CommandText()
        {
            var handler = CreateHandler();
            var command = CreateDbCommand();

            handler.Handle(new DashboardRequeueErrorMessageCommand("42"), command,
                CommandStringTypes.DashboardRequeueErrorMessage);

            Assert.NotNull(command.CommandText);
        }

        [Fact]
        public void Handle_Adds_QueueId_Parameter()
        {
            var handler = CreateHandler();
            var command = CreateDbCommand();

            handler.Handle(new DashboardRequeueErrorMessageCommand("42"), command,
                CommandStringTypes.DashboardRequeueErrorMessage);

            var parameters = (DataParameterCollection)command.Parameters;
            var param = parameters.First();
            Assert.Equal("@QueueID", param.ParameterName);
            Assert.Equal(42L, param.Value);
        }

        private static DashboardRequeueErrorMessagePrepareHandler CreateHandler()
        {
            var optionsFactory = Substitute.For<ITransportOptionsFactory>();
            optionsFactory.Create().Returns(Substitute.For<ITransportOptions>());
            return new DashboardRequeueErrorMessagePrepareHandler(new FakeCommandStringCache(), optionsFactory);
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
