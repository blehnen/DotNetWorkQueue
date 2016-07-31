// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using System.Data.SQLite;

namespace DotNetWorkQueue.Transport.SQLite.Basic.Query
{
    /// <summary>
    /// 
    /// </summary>
    public class DoesJobExistQuery: IQuery<QueueStatuses>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DoesJobExistQuery" /> class.
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="scheduledTime">The expected scheduled time.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="transaction">The transaction.</param>
        public DoesJobExistQuery(string jobName, DateTimeOffset scheduledTime, SQLiteConnection connection = null, SQLiteTransaction transaction = null)
        {
            JobName = jobName;
            ScheduledTime = scheduledTime;
            Connection = connection;
            Transaction = transaction;
        }
        /// <summary>
        /// Gets the name of the job.
        /// </summary>
        /// <value>
        /// The name of the job.
        /// </value>
        public string JobName { get; }
        /// <summary>
        /// Gets the scheduled time.
        /// </summary>
        /// <value>
        /// The scheduled time.
        /// </value>
        public DateTimeOffset ScheduledTime { get; }
        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <value>
        /// The connection.
        /// </value>
        public SQLiteConnection Connection { get; }
        /// <summary>
        /// Gets the transaction.
        /// </summary>
        /// <value>
        /// The transaction.
        /// </value>
        public SQLiteTransaction Transaction { get; }
    }
}
