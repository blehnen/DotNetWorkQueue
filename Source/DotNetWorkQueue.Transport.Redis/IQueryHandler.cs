namespace DotNetWorkQueue.Transport.Redis
{
    /// <summary>
    /// A handler for a <see cref="IQuery{TResult}"/> 
    /// </summary>
    /// <typeparam name="TQuery">The type of the query.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public interface IQueryHandler<in TQuery, out TResult> where TQuery : IQuery<TResult>
    {
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        TResult Handle(TQuery query);
    }
}
