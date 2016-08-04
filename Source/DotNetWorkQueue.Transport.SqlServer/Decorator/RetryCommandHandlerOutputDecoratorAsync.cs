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
using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Decorator
{
    internal class RetryCommandHandlerOutputDecoratorAsync<TCommand, TOutput> : ICommandHandlerWithOutputAsync<TCommand, TOutput>
    {
        private readonly ICommandHandlerWithOutputAsync<TCommand, TOutput> _decorated;
        private readonly ThreadSafeRandom _threadSafeRandom;
        private readonly ILog _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryCommandHandlerOutputDecorator{TCommand,TOutput}" /> class.
        /// </summary>
        /// <param name="decorated">The decorated.</param>
        /// <param name="log">The log.</param>
        /// <param name="threadSafeRandom">The random.</param>
        public RetryCommandHandlerOutputDecoratorAsync(ICommandHandlerWithOutputAsync<TCommand, TOutput> decorated,
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
        /// Handles the specified command, retrying up to count for specific errors
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        public async Task<TOutput> Handle(TCommand command)
        {
            Guard.NotNull(() => command, command);
            return await HandleWithCountDown(command, RetryConstants.RetryCount);
        }

        /// <summary>
        /// Handles the with count down.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        private async Task<TOutput> HandleWithCountDown(TCommand command, int count)
        {
            try
            {
                return await _decorated.Handle(command);
            }
            catch (SqlException sqlEx)
            {
                if (!Enum.IsDefined(typeof(RetryableSqlErrors), sqlEx.Number))
                    throw;

                if (count <= 0)
                    throw;

                var wait = _threadSafeRandom.Next(RetryConstants.MinWait, RetryConstants.MaxWait);
                _log.WarnException($"An error has occurred; we will try to re-run the transaction in {wait} ms", sqlEx);
                Thread.Sleep(wait);

                return await HandleWithCountDown(command, count - 1);
            }
        }
    }
}
