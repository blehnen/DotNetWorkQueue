namespace DotNetWorkQueue
{
    /// <summary>
    /// Indicates if standard options for a transport are enabled.
    /// </summary>
    public interface IBaseTransportOptions
    {
        #region Options
        /// <summary>
        /// Gets or sets a value indicating whether [enable priority].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable priority]; otherwise, <c>false</c>.
        /// </value>
        bool EnablePriority { get; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable status].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable status]; otherwise, <c>false</c>.
        /// </value>
        bool EnableStatus { get; }
        /// <summary>
        /// Gets or sets a value indicating whether [enable heart beat].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable heart beat]; otherwise, <c>false</c>.
        /// </value>
        bool EnableHeartBeat { get; }
        /// <summary>
        /// Gets or sets a value indicating whether [enable delayed processing].
        /// </summary>
        /// <value>
        /// <c>true</c> if [enable delayed processing]; otherwise, <c>false</c>.
        /// </value>
        bool EnableDelayedProcessing { get; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable status table].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable status table]; otherwise, <c>false</c>.
        /// </value>
        bool EnableStatusTable { get;}

        /// <summary>
        /// Gets or sets a value indicating whether routing is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable route]; otherwise, <c>false</c>.
        /// </value>
        bool EnableRoute { get; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable message expiration].
        /// </summary>
        /// <value>
        /// <c>true</c> if [enable message expiration]; otherwise, <c>false</c>.
        /// </value>
        bool EnableMessageExpiration { get;  }

        #endregion
    }
}
