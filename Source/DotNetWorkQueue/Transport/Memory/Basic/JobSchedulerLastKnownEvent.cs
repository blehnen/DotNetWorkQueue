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
namespace DotNetWorkQueue.Transport.Memory.Basic
{
    /// <summary>
    /// Gets and sets the last event time for scheduled jobs
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IJobSchedulerLastKnownEvent" />
    public class JobSchedulerLastKnownEvent : IJobSchedulerLastKnownEvent
    {
        private readonly IDataStorage _dataStorage;

        /// <summary>
        /// Initializes a new instance of the <see cref="JobSchedulerLastKnownEvent" /> class.
        /// </summary>
        /// <param name="dataStorage">The data storage.</param>
        public JobSchedulerLastKnownEvent(IDataStorage dataStorage)
        {
            _dataStorage = dataStorage;
        }

        /// <summary>
        /// Gets the last known event time for the specified job.
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        /// <returns></returns>
        public DateTimeOffset Get(string jobName)
        {
            return _dataStorage.GetJobLastKnownEvent(jobName);
        }
    }
}
