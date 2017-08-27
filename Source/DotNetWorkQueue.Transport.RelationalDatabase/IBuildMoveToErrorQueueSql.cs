namespace DotNetWorkQueue.Transport.RelationalDatabase
{
    public interface IBuildMoveToErrorQueueSql
    {
        /// <summary>
        /// Creates the sql statement to move a record to the error queue
        /// </summary>
        /// <returns></returns>
        string Create();
    }
}
