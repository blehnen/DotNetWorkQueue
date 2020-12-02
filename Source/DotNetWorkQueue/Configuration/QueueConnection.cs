using System.Collections.Generic;
namespace DotNetWorkQueue.Configuration
{
    /// <summary>
    /// Defines what queue to use and how to connect to it
    /// </summary>
    public class QueueConnection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueConnection"/> class.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        public QueueConnection(string queue, string connection) : this(queue, connection, null)
        {

        }
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueConnection"/> class.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="additionalConnectionSettings">The additional connection settings.</param>
        public QueueConnection(string queue, string connection, IReadOnlyDictionary<string, string> additionalConnectionSettings)
        {
            Queue = queue;
            Connection = connection;
            AdditionalConnectionSettings = additionalConnectionSettings;
        }

        /// <summary>
        /// Gets the queue.
        /// </summary>
        public string Queue { get; }
        /// <summary>
        /// Gets the connection.
        /// </summary>
        public string Connection { get; }
        /// <summary>
        /// Gets the additional connection settings.
        /// </summary>
        public IReadOnlyDictionary<string, string> AdditionalConnectionSettings { get; }
    }
}
