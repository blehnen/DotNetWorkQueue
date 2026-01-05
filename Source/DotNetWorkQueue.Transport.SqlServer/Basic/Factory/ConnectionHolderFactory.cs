// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using System;
using Microsoft.Data.SqlClient;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.Factory
{
    /// <summary>
    /// 
    /// </summary>
    public class ConnectionHolderFactory : IConnectionHolderFactory<SqlConnection, SqlTransaction, SqlCommand>
    {
        private readonly IConnectionInformation _connectionInfo;
        private readonly Lazy<SqlServerMessageQueueTransportOptions> _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionHolderFactory" /> class.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="options">The options.</param>
        public ConnectionHolderFactory(IConnectionInformation connectionInfo,
            ISqlServerMessageQueueTransportOptionsFactory options)
        {
            Guard.NotNull(() => connectionInfo, connectionInfo);
            Guard.NotNull(() => options, options);

            _connectionInfo = connectionInfo;
            _options = new Lazy<SqlServerMessageQueueTransportOptions>(options.Create);
        }
        /// <summary>
        /// Creates a new instance of <see cref="T:DotNetWorkQueue.Transport.RelationalDatabase.IConnectionHolder`3" />
        /// </summary>
        /// <returns></returns>
        public IConnectionHolder<SqlConnection, SqlTransaction, SqlCommand> Create()
        {
            return new ConnectionHolder(_connectionInfo, _options.Value);
        }
    }
}
