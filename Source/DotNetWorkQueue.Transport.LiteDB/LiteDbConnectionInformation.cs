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
using System.Collections.Generic;
using DotNetWorkQueue.Configuration;
using LiteDB;

namespace DotNetWorkQueue.Transport.LiteDb
{
    /// <inheritdoc />
    public class LiteDbConnectionInformation: BaseConnectionInformation
    {
        private readonly string _server;

        #region Constructor
        /// <inheritdoc />
        public LiteDbConnectionInformation(QueueConnection queueConnection) : base(new QueueConnection(queueConnection.Queue, GetConnection(queueConnection.Connection)))
        {
            _server = "TODO; not known";
        }
        #endregion

        #region Public Properties

        /// <inheritdoc />
        public override string Server => _server;

        /// <inheritdoc />
        public override string Container => Server;
        #endregion

        #region IClone
        /// <inheritdoc />
        public override IConnectionInformation Clone()
        {
            var data = new Dictionary<string, string>();
            foreach (var keyvalue in AdditionalConnectionSettings)
            {
                data.Add(keyvalue.Key, keyvalue.Value);
            }
            return new LiteDbConnectionInformation(new QueueConnection(QueueName, ConnectionString, data));
        }
        #endregion

        private static string GetConnection(string connectionString)
        {
            //enforce shared connection
            var connection = new ConnectionString(connectionString) {Connection = ConnectionType.Shared};

            //TODO - support all options
            return $"FileName={connection.Filename};Connection={connection.Connection};";
        }
    }
}
