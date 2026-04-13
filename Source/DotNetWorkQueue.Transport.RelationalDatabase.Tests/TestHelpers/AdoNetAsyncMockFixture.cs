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
using System.Data.Common;
using System.Threading;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using NSubstitute;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.TestHelpers
{
    /// <summary>
    /// Shared ADO.NET mock scaffolding for asynchronous handler unit tests.
    /// Uses the abstract <see cref="DbConnection"/>/<see cref="DbCommand"/>/
    /// <see cref="DbDataReader"/> base classes because <c>OpenAsync</c>,
    /// <c>ExecuteReaderAsync</c>, and <c>ReadAsync</c> are defined there.
    /// </summary>
    internal sealed class AdoNetAsyncMockFixture
    {
        public IDbConnectionFactory ConnectionFactory { get; }
        public DbConnection Connection { get; }
        public DbCommand Command { get; }
        public DbDataReader Reader { get; }
        public IReadColumn ReadColumn { get; }

        private AdoNetAsyncMockFixture()
        {
            ConnectionFactory = Substitute.For<IDbConnectionFactory>();
            Connection = Substitute.For<DbConnection>();
            Command = Substitute.For<DbCommand>();
            Reader = Substitute.For<DbDataReader>();
            ReadColumn = Substitute.For<IReadColumn>();

            ConnectionFactory.Create().Returns(Connection);
            Connection.CreateCommand().Returns(Command);
            Command.ExecuteReaderAsync(Arg.Any<CancellationToken>()).Returns(Reader);
        }

        public static AdoNetAsyncMockFixture Create() => new();

        /// <summary>
        /// Configures the reader's <see cref="DbDataReader.ReadAsync(CancellationToken)"/>
        /// to return <paramref name="rowCount"/> rows followed by a single <c>false</c>.
        /// </summary>
        public void SetupReaderRows(int rowCount)
        {
            if (rowCount <= 0)
            {
                Reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(false);
                return;
            }

            var rest = new bool[rowCount];
            for (var i = 0; i < rowCount - 1; i++) rest[i] = true;
            rest[rowCount - 1] = false;
            Reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(true, rest);
        }
    }
}
