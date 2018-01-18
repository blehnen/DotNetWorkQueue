namespace DotNetWorkQueue.Transport.Redis.Basic.Query
{
    /// <inheritdoc />
    public class GetJobIdQuery : IQuery<string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetJobIdQuery"/> class.
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        public GetJobIdQuery(string jobName)
        {
            JobName = jobName;
        }
        /// <summary>
        /// Gets the name of the job.
        /// </summary>
        /// <value>
        /// The name of the job.
        /// </value>
        public string JobName { get; }
    }
}
