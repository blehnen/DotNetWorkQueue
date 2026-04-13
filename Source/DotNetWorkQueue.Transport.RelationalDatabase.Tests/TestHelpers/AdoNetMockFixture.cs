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
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using NSubstitute;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.TestHelpers
{
    /// <summary>
    /// Shared ADO.NET mock scaffolding for synchronous handler unit tests.
    /// Wires up <see cref="IDbConnectionFactory"/>, <see cref="IDbConnection"/>,
    /// <see cref="IDbCommand"/>, and <see cref="IDataReader"/> substitutes using
    /// the interface-based <see cref="IDbCommand.ExecuteReader"/> call chain.
    /// </summary>
    internal sealed class AdoNetMockFixture
    {
        public IDbConnectionFactory ConnectionFactory { get; }
        public IDbConnection Connection { get; }
        public IDbCommand Command { get; }
        public IDataReader Reader { get; }
        public IReadColumn ReadColumn { get; }
        public ITransactionFactory TransactionFactory { get; }
        public ITransactionWrapper TransactionWrapper { get; }
        public IDbTransaction Transaction { get; }

        private AdoNetMockFixture(bool withTransaction)
        {
            ConnectionFactory = Substitute.For<IDbConnectionFactory>();
            Connection = Substitute.For<IDbConnection>();
            Command = Substitute.For<IDbCommand>();
            Reader = Substitute.For<IDataReader>();
            ReadColumn = Substitute.For<IReadColumn>();
            TransactionFactory = Substitute.For<ITransactionFactory>();
            TransactionWrapper = Substitute.For<ITransactionWrapper>();
            Transaction = Substitute.For<IDbTransaction>();

            ConnectionFactory.Create().Returns(Connection);
            Connection.CreateCommand().Returns(Command);
            Command.ExecuteReader().Returns(Reader);

            if (withTransaction)
            {
                TransactionFactory.Create(Connection).Returns(TransactionWrapper);
                TransactionWrapper.BeginTransaction().Returns(Transaction);
            }
        }

        public static AdoNetMockFixture Create(bool withTransaction = false) => new(withTransaction);

        /// <summary>
        /// Configures the reader's <see cref="IDataReader.Read"/> to return
        /// <paramref name="rowCount"/> rows followed by a single <c>false</c>.
        /// </summary>
        public void SetupReaderRows(int rowCount)
        {
            if (rowCount <= 0)
            {
                Reader.Read().Returns(false);
                return;
            }

            var rest = new bool[rowCount];
            for (var i = 0; i < rowCount - 1; i++) rest[i] = true;
            rest[rowCount - 1] = false;
            Reader.Read().Returns(true, rest);
        }
    }
}
