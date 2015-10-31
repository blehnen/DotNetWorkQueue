// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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

using System.Data.SqlClient;
using DotNetWorkQueue.Configuration;
namespace DotNetWorkQueue.Transport.SqlServer
{
    /// <summary>
    /// Contains connection information for a SQL server queue
    /// </summary>
    public class SqlConnectionInformation: BaseConnectionInformation
    {
        private string _server;

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlConnectionInformation"/> class.
        /// </summary>
        public SqlConnectionInformation()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlConnectionInformation"/> class.
        /// </summary>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="connectionString">The connection string.</param>
        protected SqlConnectionInformation(string queueName, string connectionString): base(queueName, connectionString)
        {
            SetConnection(connectionString);
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        /// <value>
        /// The connection string.
        /// </value>
        public override string ConnectionString
        {
            get { return base.ConnectionString; }
            set
            {
                FailIfReadOnly();
                SetConnection(value);
            }
        }

        /// <summary>
        /// Gets the server.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        public override string Server => _server;

        #endregion

        #region IClone
        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public override IConnectionInformation Clone()
        {
            return new SqlConnectionInformation(QueueName, ConnectionString);
        }
        #endregion

        /// <summary>
        /// Sets the connection string, based on the input value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <remarks>Connection strings that are in an invalid format will cause an exception</remarks>
        private void SetConnection(string value)
        {
            //validate that the passed in string parses as a SQL server connection string
            var builder = new SqlConnectionStringBuilder(value); //will fail here if not valid
            base.ConnectionString = builder.ToString();
            _server = builder.DataSource;
        }
    }
}
