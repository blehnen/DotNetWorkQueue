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
using System.Data.SQLite;
using DotNetWorkQueue.Logging;
using System.Threading;
using System;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Decorator
{
    internal class BeginTransactionRetryDecorator : ISqLiteTransactionWrapper
    {
        private readonly ISqLiteTransactionWrapper _decorated;
        private readonly ThreadSafeRandom _threadSafeRandom;
        private readonly ILog _log;

        public BeginTransactionRetryDecorator(ISqLiteTransactionWrapper decorated,
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
        public SQLiteConnection Connection
        {
            get { return _decorated.Connection; }
            set { _decorated.Connection = value; }
        }

        public SQLiteTransaction BeginTransaction()
        {
            return BeginTransactionWithCountDown(RetryConstants.RetryCount);
        }
        /// <summary>
        /// Handles the specified command, retrying up to count for specific errors
        /// </summary>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        private SQLiteTransaction BeginTransactionWithCountDown(int count)
        {
            try
            {
                return _decorated.BeginTransaction();
            }
            catch (SQLiteException sqlEx)
            {
                if (!Enum.IsDefined(typeof(RetryableSqlErrors), sqlEx.ErrorCode))
                    throw;

                if (count <= 0)
                    throw;

                var wait = _threadSafeRandom.Next(RetryConstants.MinWait, RetryConstants.MaxWait);
                _log.WarnException($"An error has occured; we will try to re-run the transaction in {wait} ms", sqlEx);
                Thread.Sleep(wait);

                return BeginTransactionWithCountDown(count - 1);
            }
        }
    }
}
