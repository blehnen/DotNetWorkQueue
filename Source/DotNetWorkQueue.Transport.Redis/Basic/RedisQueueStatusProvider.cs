using System;
using System.Collections.Generic;
using DotNetWorkQueue.QueueStatus;
using DotNetWorkQueue.Transport.Redis.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Status of the current queue
    /// </summary>
    internal class RedisQueueStatusProvider : QueueStatusProviderBase
    {
        private readonly IQueryHandler<GetPendingCountQuery, long> _pendingQueryHandler;
        private readonly IQueryHandler<GetPendingDelayedCountQuery, long> _pendingDelayedQueryHandler;
        private readonly IQueryHandler<GetWorkingCountQuery, long> _workingQueryHandler;
        private readonly IQueryHandler<GetErrorCountQuery, long> _errorQueryHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueStatusProvider" /> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="getTimeFactory">The get time factory.</param>
        /// <param name="pendingQueryHandler">The pending query handler.</param>
        /// <param name="pendingDelayedQueryHandler">The pending delayed query handler.</param>
        /// <param name="workingCountQueryHandler">The working count query handler.</param>
        /// <param name="errorCountQueryHandler">The error count query handler.</param>
        public RedisQueueStatusProvider(IConnectionInformation connectionInformation,
            IGetTimeFactory getTimeFactory,
            IQueryHandler<GetPendingCountQuery, long> pendingQueryHandler,
            IQueryHandler<GetPendingDelayedCountQuery, long> pendingDelayedQueryHandler,
            IQueryHandler<GetWorkingCountQuery, long> workingCountQueryHandler,
            IQueryHandler<GetErrorCountQuery, long> errorCountQueryHandler) : base(connectionInformation, getTimeFactory)
        {
            Guard.NotNull(() => pendingQueryHandler, pendingQueryHandler);
            Guard.NotNull(() => pendingDelayedQueryHandler, pendingDelayedQueryHandler);
            Guard.NotNull(() => workingCountQueryHandler, workingCountQueryHandler);
            Guard.NotNull(() => errorCountQueryHandler, errorCountQueryHandler);

            _pendingQueryHandler = pendingQueryHandler;
            _pendingDelayedQueryHandler = pendingDelayedQueryHandler;
            _workingQueryHandler = workingCountQueryHandler;
            _errorQueryHandler = errorCountQueryHandler;
        }

        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<SystemEntry> GetData()
        {
            yield return GetPendingCount();
            yield return GetPendingDelayCount();
            yield return GetWorkingCount();
            yield return GetErrorCount();
        }

        /// <summary>
        /// Gets the pending count.
        /// </summary>
        /// <returns></returns>
        private SystemEntry GetPendingCount()
        {
            const string name = "Pending";
            try
            {
                return new SystemEntry(name, _pendingQueryHandler.Handle(new GetPendingCountQuery()).ToString());
            }
            catch (Exception error)
            {
                SetError(error);
                return new SystemEntry(name, error.ToString());
            }
        }

        /// <summary>
        /// Gets the pending delay count.
        /// </summary>
        /// <returns></returns>
        private SystemEntry GetPendingDelayCount()
        {
            const string name = "DelayedPending";
            try
            {
                return new SystemEntry(name,
                    _pendingDelayedQueryHandler.Handle(new GetPendingDelayedCountQuery()).ToString());
            }
            catch (Exception error)
            {
                SetError(error);
                return new SystemEntry(name, error.ToString());
            }
        }

        /// <summary>
        /// Gets the working count.
        /// </summary>
        /// <returns></returns>
        private SystemEntry GetWorkingCount()
        {
            const string name = "Working";
            try
            {
                return new SystemEntry(name, _workingQueryHandler.Handle(new GetWorkingCountQuery()).ToString());
            }
            catch (Exception error)
            {
                SetError(error);
                return new SystemEntry(name, error.ToString());
            }
        }

        /// <summary>
        /// Gets the error count.
        /// </summary>
        /// <returns></returns>
        private SystemEntry GetErrorCount()
        {
            const string name = "Error";
            try
            {
                return new SystemEntry(name, _errorQueryHandler.Handle(new GetErrorCountQuery()).ToString());
            }
            catch (Exception error)
            {
                SetError(error);
                return new SystemEntry(name, error.ToString());
            }
        }
    }
}
