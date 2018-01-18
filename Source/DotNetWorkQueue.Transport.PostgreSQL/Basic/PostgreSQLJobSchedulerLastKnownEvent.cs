using System;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    internal class PostgreSqlJobSchedulerLastKnownEvent : IJobSchedulerLastKnownEvent
    {
        private readonly IQueryHandler<GetJobLastKnownEventQuery, DateTimeOffset> _queryGetJobTime;

        public PostgreSqlJobSchedulerLastKnownEvent(IQueryHandler<GetJobLastKnownEventQuery, DateTimeOffset> queryGetJobTime)
        {
            _queryGetJobTime = queryGetJobTime;
        }

        public DateTimeOffset Get(string jobName)
        {
            return _queryGetJobTime.Handle(new GetJobLastKnownEventQuery(jobName));
        }
    }
}
