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
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.LiteDb
{
    /// <inheritdoc />
    public class LiteDbConnectionInformation : BaseConnectionInformation
    {
        private static readonly Regex ValidQueueNamePattern = new Regex(@"^[a-zA-Z0-9_.]+$", RegexOptions.Compiled);

        private readonly string _server;

        #region Constructor
        /// <inheritdoc />
        public LiteDbConnectionInformation(QueueConnection queueConnection) : base(queueConnection)
        {
            ValidateQueueName(queueConnection.Queue);
            _server = "TODO; not known";
        }
        #endregion

        #region Public Properties

        /// <inheritdoc />
        public override string Server => _server;

        /// <inheritdoc />
        public override string Container => Server;
        #endregion

        /// <summary>Validates that the queue name contains only safe characters for use as a LiteDB collection name.</summary>
        private static void ValidateQueueName(string name)
        {
            Guard.NotNullOrEmpty(() => name, name);
            Guard.IsValid(() => name, name, n => n.Length <= 256,
                $"Queue name exceeds maximum length of 256 characters. Got {name.Length} characters.");
            Guard.IsValid(() => name, name, n => ValidQueueNamePattern.IsMatch(n),
                "Queue name contains invalid characters. Only alphanumeric characters, underscores, and dots are allowed.");
        }

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
    }
}
