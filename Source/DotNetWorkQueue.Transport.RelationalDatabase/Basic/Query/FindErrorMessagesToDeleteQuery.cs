using System.Collections.Generic;
using System.Threading;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query
{
    public class FindErrorMessagesToDeleteQuery : IQuery<IEnumerable<long>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FindErrorMessagesToDeleteQuery"/> class.
        /// </summary>
        /// <param name="cancellation">The cancellation.</param>
        public FindErrorMessagesToDeleteQuery(CancellationToken cancellation)
        {
            Guard.NotNull(() => Cancellation, Cancellation);
            Cancellation = cancellation;
        }
        /// <summary>
        /// Gets the cancellation token.
        /// </summary>
        /// <value>
        /// The cancellation.
        /// </value>
        public CancellationToken Cancellation { get; }
    }
}
