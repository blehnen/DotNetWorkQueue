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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandPrepareHandler;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryPrepareHandler
{
    public class DashboardDeleteAllErrorMessagesPrepareHandlerTests
    {
        [Fact]
        public void Handle_Sets_CommandText()
        {
            var handler = CreateHandler();
            var command = CreateDbCommand();

            handler.Handle(new DashboardDeleteAllErrorMessagesCommand(), command,
                CommandStringTypes.DashboardDeleteAllErrors_MetaData);

            Assert.NotNull(command.CommandText);
        }

        [Fact]
        public void Handle_Does_Not_Add_Parameters()
        {
            var handler = CreateHandler();
            var command = CreateDbCommand();

            handler.Handle(new DashboardDeleteAllErrorMessagesCommand(), command,
                CommandStringTypes.DashboardDeleteAllErrors_MetaData);

            var parameters = (DataParameterCollection)command.Parameters;
            Assert.Empty(parameters);
        }

        private static DashboardDeleteAllErrorMessagesPrepareHandler CreateHandler()
        {
            return new DashboardDeleteAllErrorMessagesPrepareHandler(new FakeCommandStringCache());
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
