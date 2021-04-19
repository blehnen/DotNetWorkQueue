// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Transport.RelationalDatabase.ITransactionFactory" />
    internal class TransactionFactory : ITransactionFactory
    {
        private readonly IDbFactory _factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionFactory"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        public TransactionFactory(IDbFactory factory)
        {
            Guard.NotNull(() => factory, factory);
            _factory = factory;
        }
        /// <summary>
        /// Creates a new instance of <seealso cref="T:DotNetWorkQueue.Transport.RelationalDatabase.ITransactionWrapper" />
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public ITransactionWrapper Create(IDbConnection connection)
        {
            return _factory.CreateTransaction(connection);
        }
    }
}
