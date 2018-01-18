using System.Collections.Generic;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Tracks the exceptions that have occurred in user code while processing a message
    /// </summary>
    public class RedisErrorTracking
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisErrorTracking"/> class.
        /// </summary>
        public RedisErrorTracking()
        {
            Errors = new Dictionary<string, int>();
        }

        /// <summary>
        /// Gets the errors.
        /// </summary>
        /// <value>
        /// The errors.
        /// </value>
        /// <remarks>This is public, otherwise it may not be serialized, depending on the engine the user has chosen</remarks>
        public Dictionary<string, int> Errors{ get; set; }

        /// <summary>
        /// Gets the exception count.
        /// </summary>
        /// <param name="exceptionType">Type of the exception.</param>
        /// <returns></returns>
        public int GetExceptionCount(string exceptionType)
        {
            return !Errors.ContainsKey(exceptionType) ? 0 : Errors[exceptionType];
        }

        /// <summary>
        /// Increments the exception count.
        /// </summary>
        /// <param name="exceptionType">Type of the exception.</param>
        public void IncrementExceptionCount(string exceptionType)
        {
            if (!Errors.ContainsKey(exceptionType))
            {
                Errors.Add(exceptionType, 1);
            }
            else
            {
                Errors[exceptionType] = Errors[exceptionType] + 1;
            }
        }
    }
}
