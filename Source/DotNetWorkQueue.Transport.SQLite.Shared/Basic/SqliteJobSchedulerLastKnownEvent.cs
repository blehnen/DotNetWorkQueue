using System;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic
{
    /// <summary>
    /// Gets and sets the last event time for scheduled jobs
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IJobSchedulerLastKnownEvent" />
    public class SqliteJobSchedulerLastKnownEvent : IJobSchedulerLastKnownEvent
    {
        private readonly IQueryHandler<GetJobLastKnownEventQuery, DateTimeOffset> _queryGetJobTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteJobSchedulerLastKnownEvent" /> class.
        /// </summary>
        /// <param name="queryGetJobTime">The query get job time.</param>
        public SqliteJobSchedulerLastKnownEvent(IQueryHandler<GetJobLastKnownEventQuery, DateTimeOffset> queryGetJobTime)
        {
            _queryGetJobTime = queryGetJobTime;
        }

        /// <summary>
        /// Gets the last known event time for the specified job.
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        /// <returns></returns>
        public DateTimeOffset Get(string jobName)
        {
            return _queryGetJobTime.Handle(new GetJobLastKnownEventQuery(jobName));
        }
    }
}
