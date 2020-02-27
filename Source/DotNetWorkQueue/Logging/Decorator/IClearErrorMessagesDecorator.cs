using System.Threading;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Logging.Decorator
{
    internal class ClearErrorMessagesDecorator : IClearErrorMessages
    {
        private readonly ILog _log;
        private readonly IClearErrorMessages _handler;
        private readonly IConnectionInformation _connectionInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClearExpiredMessagesDecorator" /> class.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="connectionInfo">The connection information.</param>
        public ClearErrorMessagesDecorator(ILogFactory log,
            IClearErrorMessages handler,
            IConnectionInformation connectionInfo)
        {
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => handler, handler);
            Guard.NotNull(() => connectionInfo, connectionInfo);

            _log = log.Create();
            _handler = handler;
            _connectionInfo = connectionInfo;
        }

        /// <inheritdoc />
        public long ClearMessages(CancellationToken cancelToken)
        {
            var count = _handler.ClearMessages(cancelToken);
            if (count > 0)
            {
                _log.Info($"Deleted {count} error messages from {_connectionInfo.QueueName}");
            }
            return count;
        }
    }
}
