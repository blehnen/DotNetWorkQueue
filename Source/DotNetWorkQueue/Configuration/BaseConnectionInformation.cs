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

using System;
namespace DotNetWorkQueue.Configuration
{
    /// <summary>
    /// Defines a connection to a queue
    /// </summary>
    public class BaseConnectionInformation : IConnectionInformation
    {
        private string _connectionString;
        private string _queueName;
        private int _hashCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseConnectionInformation"/> class.
        /// </summary>
        public BaseConnectionInformation()
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseConnectionInformation"/> class.
        /// </summary>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="connectionString">The connection string.</param>
        protected BaseConnectionInformation(string queueName, string connectionString)
        {
            _queueName = queueName;
            _connectionString = connectionString;
        }

        #region Public Properties
        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        /// <value>
        /// The connection string.
        /// </value>
        public virtual string ConnectionString
        {
            get
            {
                return _connectionString;
            }
            set
            {
                FailIfReadOnly();
                _connectionString = value;
                _hashCode = CalculateHashCode();
            }
        }
        /// <summary>
        /// Gets or sets the name of the queue.
        /// </summary>
        /// <value>
        /// The name of the queue.
        /// </value>
        public virtual string QueueName
        {
            get { return _queueName; }
            set
            {
                FailIfReadOnly();
                _queueName = value;
                _hashCode = CalculateHashCode();
            }
        }
        /// <summary>
        /// Gets the server.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        /// <remarks>The server display name</remarks>
        public virtual string Server => "Base connection object cannot determine server";

        #endregion

        #region IClone
        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public virtual IConnectionInformation Clone()
        {
            return new BaseConnectionInformation(QueueName, ConnectionString);
        }
        #endregion

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Join("|", _connectionString, _queueName);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            // Check for null values and compare run-time types.
            if (obj == null || GetType() != obj.GetType())
                return false;

            var connection = (BaseConnectionInformation)obj;
            return (_connectionString == connection.ConnectionString && _queueName == connection.QueueName);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            return _hashCode;
        }

        /// <summary>
        /// Throws an exception if the readonly flag is true.
        /// </summary>
        /// <exception cref="System.Data.ReadOnlyException"></exception>
        protected void FailIfReadOnly()
        {
            if (IsReadOnly) throw new InvalidOperationException();
        }

        /// <summary>
        /// Calculates the hash code.
        /// </summary>
        /// <returns></returns>
        protected int CalculateHashCode()
        {
            return string.Concat(_connectionString, _queueName).GetHashCode();
        }

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is read only; otherwise, <c>false</c>.
        /// </value>
        public bool IsReadOnly { get; private set; }
        /// <summary>
        /// Marks this instance as imutable
        /// </summary>
        public void SetReadOnly()
        {
            IsReadOnly = true;
        }
    }
}
