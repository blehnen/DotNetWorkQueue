// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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
using DotNetWorkQueue.Logging;
namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Message processor for non-async queues
    /// </summary>
    internal class Worker : MultiWorkerBase, IWorker
    {
        private readonly ILog _log;
        private readonly IWorkerWaitForEventOrCancel _pauseEvent;
        private readonly IWorkerNameFactory _nameFactory;
        private readonly IMessageProcessingFactory _messageProcessingFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="Worker" /> class.
        /// </summary>
        /// <param name="nameFactory">The name factory.</param>
        /// <param name="pauseEvent">The pause event.</param>
        /// <param name="log">The log.</param>
        /// <param name="messageProcessing">The message processing.</param>
        /// <param name="workerTerminate">The worker terminate.</param>
        /// <param name="stopThead">The stop thead.</param>
        public Worker(
            IWorkerNameFactory nameFactory,
            IWorkerWaitForEventOrCancel pauseEvent,
            ILogFactory log,
            IMessageProcessingFactory messageProcessing,
            WorkerTerminate workerTerminate,
            StopThread stopThead)
            : base(workerTerminate, stopThead)
        {
            Guard.NotNull(() => pauseEvent, pauseEvent);
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => nameFactory, nameFactory);

            _pauseEvent = pauseEvent;
            _log = log.Create();
            _nameFactory = nameFactory;
            _messageProcessingFactory = messageProcessing;
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
            MessageProcessing.SystemException += RaiseSystemMessageException;
            MessageProcessing.UserException += RaiseUserMessageException;
            MessageProcessing.Idle += (sender, e) => IdleStatus = WorkerIdleStatus.Idle;
            MessageProcessing.NotIdle += (sender, e) => IdleStatus = WorkerIdleStatus.NotIdle;

            WorkerThread = new Thread(MainLoop) { Name = _nameFactory.Create() };
            WorkerThread.Start();

            _log.Debug($"{WorkerThread.Name} created");
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public override void Stop()
        {
            if (ShouldExit) return;

            ShouldExit = true;

            if (WorkerThread == null)
                return;

            _log.DebugFormat("Stopping worker thread {0}", WorkerThread.Name);
        }

        /// <summary>
        /// Looks for messages to process
        /// </summary>
        private void MainLoop()
        {
            while (!ShouldExit)
            {
                //wait, if told to do so
                _pauseEvent.Wait();
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
