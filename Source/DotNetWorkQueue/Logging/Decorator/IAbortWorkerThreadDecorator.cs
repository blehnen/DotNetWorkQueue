// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
using System.Threading;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Logging.Decorator
{
    internal class AbortWorkerThreadDecorator: IAbortWorkerThread
    {
        private readonly ILog _log;
        private readonly IAbortWorkerThread _handler;
        private readonly IWorkerConfiguration _configuration;
        private readonly MessageProcessingMode _messageMode;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbortWorkerThreadDecorator" /> class.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="messageMode">The message mode.</param>
        /// <param name="handler">The handler.</param>
        public AbortWorkerThreadDecorator(ILogFactory log,
            IWorkerConfiguration configuration,
            MessageProcessingMode messageMode,
            IAbortWorkerThread handler)
        {
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => handler, handler);
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => messageMode, messageMode);

            _log = log.Create();
            _handler = handler;
            _configuration = configuration;
            _messageMode = messageMode;
        }

        /// <summary>
        /// Aborts the specified worker thread, if configured to do so.
        /// </summary>
        /// <param name="workerThread">The worker thread.</param>
        /// <returns>
        /// True if thread aborted; false otherwise
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public bool Abort(Thread workerThread)
        {
            //log a warning message if we are in async mode, and the abort flag is true
            if (_messageMode.Mode == MessageProcessingModes.Async && _configuration.AbortWorkerThreadsWhenStopping)
            {
                _log.WarnFormat(
                "AbortWorkerThreadsWhenStopping is true, but we are running in async mode. Async threads cannot be aborted.",
               workerThread.Name);
            }

            var aborted = _handler.Abort(workerThread);
            if (aborted)
            {
                _log.WarnFormat(
                    "Worker thread {0} was aborted due to not responding to a stop and a cancel request",
                    workerThread.Name);
            }
            return aborted;
        }
    }
}
