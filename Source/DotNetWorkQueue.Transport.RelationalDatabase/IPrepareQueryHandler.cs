using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;

namespace DotNetWorkQueue.Transport.RelationalDatabase
{
    public interface IPrepareQueryHandler<in TQuery, out TResult> where TQuery : IQuery<TResult>
    {
        void Handle(TQuery query, IDbCommand dbCommand, CommandStringTypes commandType);
    }
}
