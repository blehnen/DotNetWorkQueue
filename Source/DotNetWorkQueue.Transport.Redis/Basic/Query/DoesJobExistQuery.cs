using System;
using DotNetWorkQueue.Transport.Shared;

namespace DotNetWorkQueue.Transport.Redis.Basic.Query
{
    /// <inheritdoc />
    public class DoesJobExistQuery: IQuery<QueueStatuses>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DoesJobExistQuery" /> class.
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="scheduledTime">The expected scheduled time.</param>
        public DoesJobExistQuery(string jobName, DateTimeOffset scheduledTime)
        {
            JobName = jobName;
            ScheduledTime = scheduledTime;
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
    }
}
