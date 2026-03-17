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
