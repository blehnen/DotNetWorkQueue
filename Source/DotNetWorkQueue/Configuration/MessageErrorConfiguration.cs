using System;
namespace DotNetWorkQueue.Configuration
{
    public class MessageErrorConfiguration: IMessageErrorConfiguration
    {
        private TimeSpan _monitorTime;
        private TimeSpan _messageAge;
        private bool _enabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageErrorConfiguration"/> class.
        /// </summary>
        public MessageErrorConfiguration()
        {
            Enabled = true; //enable by default, as queues filled with error messages will slow down
            MessageAge = TimeSpan.FromDays(30); //default to 30 days
            MonitorTime = TimeSpan.FromDays(1); //check every day
        }

        #region Configuration
        /// <inheritdoc />
        public TimeSpan MonitorTime
        {
            get => _monitorTime;
            set
            {
                FailIfReadOnly();
                _monitorTime = value;
            }
        }

        /// <inheritdoc />
        public bool Enabled
        {
            get => _enabled;
            set
            {
                FailIfReadOnly();
                _enabled = value;
            }
        }

        /// <inheritdoc />
        public TimeSpan MessageAge
        {
            get => _messageAge;
            set
            {
                FailIfReadOnly();
                _messageAge = value;
            }
        }

        /// <inheritdoc />
        public bool IsReadOnly { get; protected set; }

        /// <summary>
        /// Throws an exception if the read only flag is true.
        /// </summary>
        /// <exception cref="System.Data.ReadOnlyException"></exception>
        protected void FailIfReadOnly()
        {
            if (IsReadOnly) throw new InvalidOperationException();
        }

        /// <inheritdoc />
        public void SetReadOnly()
        {
            IsReadOnly = true;
        }
        #endregion
    }
}
