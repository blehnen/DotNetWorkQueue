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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Consumes linq expression methods.
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Queue.BaseQueue" />
    /// <seealso cref="DotNetWorkQueue.IConsumerMethodQueue" />
    public class ConsumerMethodQueue : BaseQueue, IConsumerMethodQueue
    {
        private readonly IConsumerQueue _queue;
        private readonly IMessageMethodHandling _messageMethodHandling;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsumerMethodQueue"/> class.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="log">The log.</param>
        /// <param name="messageMethodHandling">The message method handling.</param>
        public ConsumerMethodQueue(
           IConsumerQueue queue,
           ILogFactory log,
           IMessageMethodHandling messageMethodHandling)
           : base(log)
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
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;

            _queue.Dispose();
            _messageMethodHandling.Dispose();

            base.Dispose(true);
        }
    }
}
