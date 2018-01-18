// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
using System.Threading;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Represents the primary worker; all non-async queues have one of these.
    /// </summary>
    internal class PrimaryWorker : MultiWorkerBase, IPrimaryWorker
    {
        private readonly ILog _log;
        private readonly IWorkerNameFactory _nameFactory;
        private readonly IWorkerCollection _workerCollection;
        private readonly IMessageProcessingFactory _messageProcessingFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrimaryWorker" /> class.
        /// </summary>
        /// <param name="nameFactory">The name factory.</param>
        /// <param name="log">The log.</param>
        /// <param name="messageProcessing">The message processing.</param>
        /// <param name="workerTerminate">The worker terminate.</param>
        /// <param name="workerCollection">The worker collection.</param>
        /// <param name="stopThread">The stop thread.</param>
        public PrimaryWorker(
            IWorkerNameFactory nameFactory,
            ILogFactory log,
            IMessageProcessingFactory messageProcessing,
            WorkerTerminate workerTerminate,
            IWorkerCollection workerCollection,
            StopThread stopThread)
            : base(workerTerminate, stopThread)
        {
            Guard.NotNull(() => nameFactory, nameFactory);
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => messageProcessing, messageProcessing);
            Guard.NotNull(() => workerTerminate, workerTerminate);
            Guard.NotNull(() => workerCollection, workerCollection);

            _log = log.Create();
            _nameFactory = nameFactory;
            _messageProcessingFactory = messageProcessing;
            _workerCollection = workerCollection;

            IdleStatus = WorkerIdleStatus.Unknown;
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        public override void Start()
        {
            if (ShouldExit) return;

            if (WorkerThread != null) return;

            MessageProcessing = _messageProcessingFactory.Create();

            _workerCollection.SystemException += RaiseSystemException;
            _workerCollection.UserException += RaiseUserException;
            MessageProcessing.SystemException += RaiseSystemMessageException;
            MessageProcessing.UserException += RaiseUserMessageException;
            MessageProcessing.Idle += (sender, e) => IdleStatus = WorkerIdleStatus.Idle;
            MessageProcessing.NotIdle += (sender, e) => IdleStatus = WorkerIdleStatus.NotIdle;
            MessageProcessing.NotIdle += MessageProcessingOnNotIdle;

            WorkerThread = new Thread(MainLoop) { Name = _nameFactory.Create() };
            WorkerThread.Start();

            _log.Debug($"{WorkerThread.Name} created");

            _workerCollection.Start();
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public override void Stop()
        {
            if (ShouldExit) return;

            if (WorkerThread != null)
            {
                _log.DebugFormat("Stopping worker thread {0}", WorkerThread.Name);
            }

            _workerCollection.Stop();
            _workerCollection.SystemException -= RaiseSystemException;
            _workerCollection.UserException -= RaiseUserException;

            ShouldExit = true;
        }

        /// <summary>
        /// Determines the action to take when the message processing module indicates it's not idle.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void MessageProcessingOnNotIdle(object sender, EventArgs e)
        {
            //ensure that if workers have been paused, they are clear to start looking for work
            _workerCollection.ResumeWorkers();
        }

        /// <summary>
        /// Main processing loop for internal thread.
        /// </summary>
        private void MainLoop()
        {
            while (!ShouldExit)
            {              
                //if:
                //1) Single worker is allowed when idle
                //2) This worker is idle
                //3) All other workers are idle
                // Then: pause all other workers, and switch to single worker mode
                if (_workerCollection.Configuration.SingleWorkerWhenNoWorkFound && IdleStatus == WorkerIdleStatus.Idle && _workerCollection.AllWorkersAreIdle)
                {
                    _workerCollection.PauseWorkers();
                }

                if (ShouldExit)
                {
                    return;
                }
                MessageProcessing.Handle();
            }

            if (MessageProcessing != null && MessageProcessing.AsyncTaskCount > 0)
            {
                WaitOnAsyncTask.Wait(() => MessageProcessing.AsyncTaskCount > 0,
                    () => _log.Warn(
                        $"Unable to terminate because async requests have not finished. Current task count is {MessageProcessing.AsyncTaskCount}"));
            }
        }
    }
}
