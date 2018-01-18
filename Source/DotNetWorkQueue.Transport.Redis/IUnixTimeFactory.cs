namespace DotNetWorkQueue.Transport.Redis
{
    /// <summary>
    /// Returns an instance of <see cref="IUnixTime"/>
    /// </summary>
    public interface IUnixTimeFactory
    {
        /// <summary>
        /// Returns an instance of <see cref="IUnixTime"/>
        /// </summary>
        /// <returns></returns>
        IUnixTime Create();
    }
}
