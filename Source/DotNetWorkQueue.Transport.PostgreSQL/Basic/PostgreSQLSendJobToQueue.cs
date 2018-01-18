using System;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <inheritdoc />
    public class PostgreSqlSendJobToQueue : ASendJobToQueue
    {
        private readonly IQueryHandler<DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>, QueueStatuses> _doesJobExist;
        private readonly ICommandHandlerWithOutput<DeleteMessageCommand, long> _deleteMessageCommand;
        private readonly IQueryHandler<GetJobIdQuery, long> _getJobId;
        private readonly CreateJobMetaData _createJobMetaData;

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlSendJobToQueue" /> class.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="doesJobExist">Query for determining if a job already exists</param>
        /// <param name="deleteMessageCommand">The delete message command.</param>
        /// <param name="getJobId">The get job identifier.</param>
        /// <param name="createJobMetaData">The create job meta data.</param>
        /// <param name="getTimeFactory">The get time factory.</param>
        public PostgreSqlSendJobToQueue(IProducerMethodQueue queue, IQueryHandler<DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>, QueueStatuses> doesJobExist,
            ICommandHandlerWithOutput<DeleteMessageCommand, long> deleteMessageCommand,
            IQueryHandler<GetJobIdQuery, long> getJobId,
            CreateJobMetaData createJobMetaData,
            IGetTimeFactory getTimeFactory) : base(queue, getTimeFactory)
        {
            _doesJobExist = doesJobExist;
            _deleteMessageCommand = deleteMessageCommand;
            _getJobId = getJobId;
            _createJobMetaData = createJobMetaData;
        }

        /// <inheritdoc />
        protected override QueueStatuses DoesJobExist(string name, DateTimeOffset scheduledTime)
        {
            return _doesJobExist.Handle(new DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>(name, scheduledTime));
        }

        /// <inheritdoc />
        protected override void DeleteJob(string name)
        {
            _deleteMessageCommand.Handle(new DeleteMessageCommand(_getJobId.Handle(new GetJobIdQuery(name))));
        }

        /// <inheritdoc />
        protected override bool JobAlreadyExistsError(Exception error)
        {
            var message = error.Message.Replace(Environment.NewLine, " ");
            return message.Contains("duplicate key value violates unique constraint") && message.Contains("jobname") || message.Contains("Failed to insert record - the job has already been queued or processed");
        }

        /// <inheritdoc />
        protected override void SetMetaDataForJob(string jobName, DateTimeOffset scheduledTime, DateTimeOffset eventTime,
            string route, IAdditionalMessageData messageData)
        {
            _createJobMetaData.Create(jobName, scheduledTime, eventTime, messageData, route);
        }
    }
}
