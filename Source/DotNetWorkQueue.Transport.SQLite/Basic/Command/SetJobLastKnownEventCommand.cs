// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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

namespace DotNetWorkQueue.Transport.SQLite.Basic.Command
{
    /// <summary>
    /// 
    /// </summary>
    public class SetJobLastKnownEventCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SetJobLastKnownEventCommand" /> class.
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="jobEventTime">The time.</param>
        /// <param name="jobScheduledTime">The job scheduled time.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="transaction">The transaction.</param>
        public SetJobLastKnownEventCommand(string jobName, 
            DateTimeOffset jobEventTime, 
            DateTimeOffset jobScheduledTime, 
            SQLiteConnection connection,
            SQLiteTransaction transaction)
        {
            JobName = jobName;
            JobEventTime = jobEventTime;
            JobScheduledTime = jobScheduledTime;
            Connection = connection;
            Transaction = transaction;
        }
        /// <summary>
        /// Gets the name of the job.
        /// </summary>
        /// <value>
        /// The name of the job.
        /// </value>
        public string JobName { get; private set; }
        /// <summary>
        /// Gets the time.
        /// </summary>
        /// <value>
        /// The time.
        /// </value>
        public DateTimeOffset JobEventTime { get; private set; }
        /// <summary>
        /// Gets the job scheduled time.
        /// </summary>
        /// <value>
        /// The job scheduled time.
        /// </value>
        public DateTimeOffset JobScheduledTime { get; private set; }
        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <value>
        /// The connection.
        /// </value>
        public SQLiteConnection Connection { get; private set; }
        /// <summary>
        /// Gets the transaction.
        /// </summary>
        /// <value>
        /// The transaction.
        /// </value>
        public SQLiteTransaction Transaction { get; private set; }
    }
}
