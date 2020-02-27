using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Queue
{
    [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "not needed")]
    public class ClearErrorMessagesMonitor : BaseMonitor, IClearErrorMessagesMonitor
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="ClearExpiredMessagesMonitor" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="clearErrorMessages">The clear messages implementation.</param>
        /// <param name="log">The log.</param>
        public ClearErrorMessagesMonitor(IMessageErrorConfiguration configuration,
            IClearErrorMessages clearErrorMessages, ILogFactory log)
            : base(Guard.NotNull(() => clearErrorMessages, clearErrorMessages).ClearMessages, configuration, log)
        {

        }
        #endregion
    }
}
