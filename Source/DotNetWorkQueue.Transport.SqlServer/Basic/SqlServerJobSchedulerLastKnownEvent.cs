using System;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;

namespace DotNetWorkQueue.Transport.SqlServer.Basic
{
    internal class SqlServerJobSchedulerLastKnownEvent : IJobSchedulerLastKnownEvent
    {
        private readonly IQueryHandler<GetJobLastKnownEventQuery, DateTimeOffset> _queryGetJobTime;

        public SqlServerJobSchedulerLastKnownEvent(IQueryHandler<GetJobLastKnownEventQuery, DateTimeOffset> queryGetJobTime)
        {
            _queryGetJobTime = queryGetJobTime;
        }

        public DateTimeOffset Get(string jobName)
        {
            return _queryGetJobTime.Handle(new GetJobLastKnownEventQuery(jobName));
        }
    }
}
