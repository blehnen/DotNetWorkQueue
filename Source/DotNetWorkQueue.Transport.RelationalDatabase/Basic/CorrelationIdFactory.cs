using System;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.ICorrelationIdFactory" />
    public class CorrelationIdFactory : ICorrelationIdFactory
    {
        /// <summary>
        /// Creates a new instance of <see cref="T:DotNetWorkQueue.ICorrelationId" />
        /// </summary>
        /// <returns></returns>
        public ICorrelationId Create()
        {
            return new MessageCorrelationId(Guid.NewGuid());
        }
    }
}
