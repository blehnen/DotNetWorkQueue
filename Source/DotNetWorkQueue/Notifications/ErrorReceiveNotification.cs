using System;

namespace DotNetWorkQueue.Notifications
{
    /// <summary>
    /// An error obtaining messages from the transport
    /// </summary>
    public class ErrorReceiveNotification
    {
        /// <summary>
        /// An error obtaining messages from the transport
        /// </summary>
        /// <param name="error">The error.</param>
        public ErrorReceiveNotification(Exception error)
        {
            Error = error;
        }

        /// <summary>
        /// The exception that occurred.
        /// </summary>
        public Exception Error { get; }
    }
}
