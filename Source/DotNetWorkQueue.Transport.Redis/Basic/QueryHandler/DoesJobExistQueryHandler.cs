using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using DotNetWorkQueue.Transport.Redis.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.QueryHandler
{
    /// <inheritdoc />
    public class DoesJobExistQueryHandler : IQueryHandler<DoesJobExistQuery, QueueStatuses>
    {
        private readonly DoesJobExistLua _lua;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetErrorCountQueryHandler" /> class.
        /// </summary>
        /// <param name="lua">The lua.</param>
        public DoesJobExistQueryHandler(DoesJobExistLua lua)
        {
            Guard.NotNull(() => lua, lua);
            _lua = lua;
        }

        /// <inheritdoc />
        public QueueStatuses Handle(DoesJobExistQuery query)
        {
            return _lua.Execute(query.JobName, query.ScheduledTime);
        }
    }
}
