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

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query
{
    /// <summary>
    /// Dashboard query result: a scheduled job record.
    /// </summary>
    public class DashboardJob
    {
        /// <summary>
        /// Gets or sets the job name.
        /// </summary>
        public string JobName { get; set; }

        /// <summary>
        /// Gets or sets the last known event time for the job.
        /// </summary>
        public DateTimeOffset? JobEventTime { get; set; }

        /// <summary>
        /// Gets or sets the last scheduled time for the job.
        /// </summary>
        public DateTimeOffset? JobScheduledTime { get; set; }
    }
}
