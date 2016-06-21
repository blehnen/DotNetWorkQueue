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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue.TaskScheduling
{
    /// <summary>
    /// Consumes linq expression methods using the task scheduler for processing.
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IConsumerMethodQueueScheduler" />
    public class SchedulerMethod : IConsumerMethodQueueScheduler
    {
        private readonly IConsumerQueueScheduler _queue;
        private readonly IMessageMethodHandling _messageMethodHandling;

        /// <summary>
        /// Initializes a new instance of the <see cref="SchedulerMethod"/> class.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="messageMethodHandling">The message method handling.</param>
        public SchedulerMethod(IConsumerQueueScheduler queue,
            IMessageMethodHandling messageMethodHandling)
        {
            Guard.NotNull(() => queue, queue);
            Guard.NotNull(() => messageMethodHandling, messageMethodHandling);

            _queue = queue;
            _messageMethodHandling = messageMethodHandling;
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public QueueConsumerConfiguration Configuration => _queue.Configuration;

        /// <summary>
        /// Starts the queue.
        /// </summary>
        /// <remarks>
        /// Call dispose to stop the queue once started
        /// </remarks>
        public void Start()
        {
            _queue.Start<MessageExpression>(_messageMethodHandling.HandleExecution);
        }

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed => _queue.IsDisposed;

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _queue.Dispose();
                    _messageMethodHandling.Dispose();
                }
                _disposedValue = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
