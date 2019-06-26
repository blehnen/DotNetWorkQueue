using System.Collections.Generic;
namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Gets the default header
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IGetHeader" />
    public class GetHeaderDefault: IGetHeader
    {
        /// <summary>
        /// Gets the headers for the specified message if possible
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>
        /// null if the headers could not be obtained; otherwise a collection with 0 or more records
        /// </returns>
        /// <exception cref="DotNetWorkQueue.Exceptions.DotNetWorkQueueException">Transports must implement IGetHeader</exception>
        public IDictionary<string, object> GetHeaders(IMessageId id)
        {
            throw new Exceptions.DotNetWorkQueueException("Transports must implement IGetHeader");
        }
    }
}
