// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Delegate-based implementation of <see cref="IConsumerMetricsNotification"/>.
    /// Allows wiring metric counters without taking a dependency on a specific metrics library.
    /// </summary>
    public class ConsumerMetricsNotification : IConsumerMetricsNotification
    {
        private readonly Action _onProcessed;
        private readonly Action _onErrored;
        private readonly Action _onRolledBack;
        private readonly Action _onPoisonMessage;

        /// <summary>
        /// Initializes a new instance with the specified callback actions.
        /// </summary>
        /// <param name="onProcessed">Called when a message is successfully processed.</param>
        /// <param name="onErrored">Called when a message processing error occurs.</param>
        /// <param name="onRolledBack">Called when a message is rolled back.</param>
        /// <param name="onPoisonMessage">Called when a poison message is detected.</param>
        public ConsumerMetricsNotification(Action onProcessed, Action onErrored, Action onRolledBack, Action onPoisonMessage)
        {
            _onProcessed = onProcessed ?? throw new ArgumentNullException(nameof(onProcessed));
            _onErrored = onErrored ?? throw new ArgumentNullException(nameof(onErrored));
            _onRolledBack = onRolledBack ?? throw new ArgumentNullException(nameof(onRolledBack));
            _onPoisonMessage = onPoisonMessage ?? throw new ArgumentNullException(nameof(onPoisonMessage));
        }

        /// <inheritdoc />
        public void IncrementProcessed() => _onProcessed();

        /// <inheritdoc />
        public void IncrementErrored() => _onErrored();

        /// <inheritdoc />
        public void IncrementRolledBack() => _onRolledBack();

        /// <inheritdoc />
        public void IncrementPoisonMessage() => _onPoisonMessage();
    }
}
