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
using System.Linq;
using System.Threading;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Validation;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Decorator
{
    internal class RetryQueryHandlerDecorator<TQuery, TResult> : IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
    {
        private readonly IQueryHandler<TQuery, TResult> _decorated;
        private readonly ThreadSafeRandom _threadSafeRandom;
        private readonly ILog _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="DotNetWorkQueue.Transport.PostgreSQL.Decorator.RetryQueryHandlerDecorator{TQuery,TResult}" /> class.
        /// </summary>
        /// <param name="decorated">The decorated.</param>
        /// <param name="log">The log.</param>
        /// <param name="threadSafeRandom">The random.</param>
        public RetryQueryHandlerDecorator(IQueryHandler<TQuery, TResult> decorated, 
            ILogFactory log, 
            ThreadSafeRandom threadSafeRandom)
        {
            Guard.NotNull(() => decorated, decorated);
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => threadSafeRandom, threadSafeRandom);

            _decorated = decorated;
            _log = log.Create();
            _threadSafeRandom = threadSafeRandom;
        }

        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public TResult Handle(TQuery query)
        {
            Guard.NotNull(() => query, query);
            return HandleWithCountDown(query, RetryConstants.RetryCount);
        }

        /// <summary>
        /// Handles the specified query, retrying up to count for specific errors
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        private TResult HandleWithCountDown(TQuery query, int count)
        {
            try
            {
                return _decorated.Handle(query);
            }
            catch (PostgresException sqlEx)
            {
                if (!RetryablePostGreErrors.Errors.Contains(sqlEx.SqlState))
                    throw;

                if (count <= 0)
                    throw;

                var wait = _threadSafeRandom.Next(RetryConstants.MinWait, RetryConstants.MaxWait);
                _log.WarnException($"An error has occurred; we will try to re-run the transaction in {wait} ms", sqlEx);
                Thread.Sleep(wait);

                return HandleWithCountDown(query, count - 1);
            }
        }
    }
}
