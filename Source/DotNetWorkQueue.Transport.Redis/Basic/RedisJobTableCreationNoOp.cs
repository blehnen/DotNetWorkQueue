namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <inheritdoc />
    internal class RedisJobTableCreationNoOp: IJobTableCreation
    {
        /// <inheritdoc />
        public bool JobTableExists => true;

        /// <inheritdoc />
        public QueueCreationResult CreateJobTable()
        {
            return new QueueCreationResult(QueueCreationStatus.Success);
        }
    }
}
