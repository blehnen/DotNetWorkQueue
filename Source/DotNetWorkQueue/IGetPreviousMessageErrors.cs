using System.Collections.Generic;
namespace DotNetWorkQueue
{
    public interface IGetPreviousMessageErrors
    {
        /// <summary>
        /// Returns any error messages associated with the message from transport storage
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        IReadOnlyDictionary<string, int> Get(IMessageId id);
    }
}
