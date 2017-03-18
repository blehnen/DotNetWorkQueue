// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Aborts a thread, if configured to do so. Otherwise, no actions are taken.
    /// </summary>
    public class AbortWorkerThread : IAbortWorkerThread
    {
        private readonly IWorkerConfiguration _configuration;
        private readonly MessageProcessingMode _messageMode;
        /// <summary>
        /// Initializes a new instance of the <see cref="AbortWorkerThread" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="messageMode">The message mode.</param>
        public AbortWorkerThread(IWorkerConfiguration configuration, 
            MessageProcessingMode messageMode)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => messageMode, messageMode);
           
            _configuration = configuration;
            _messageMode = messageMode;
        }
        /// <summary>
        /// Aborts the specified worker thread, if configured to do so.
        /// </summary>
        /// <param name="workerThread">The worker thread.</param>
        /// <returns>True if thread aborted; false otherwise</returns>
        public bool Abort(Thread workerThread)
        {
            //return if we never abort worker threads
            if (!_configuration.AbortWorkerThreadsWhenStopping) return false;
            
            //return if the worker is already dead
            if (workerThread == null || !workerThread.IsAlive) return true;

            //we can't abort async threads
            if (_messageMode.Mode == MessageProcessingModes.Async)
            {
                return false;
            }

            //abort the thread... :(
            workerThread.Abort();
            
            return true;
        }
    }
}
