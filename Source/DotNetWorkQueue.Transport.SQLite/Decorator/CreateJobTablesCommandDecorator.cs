using System.Data.SQLite;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Decorator
{
    internal class CreateJobTablesCommandDecorator : ICommandHandlerWithOutput<CreateJobTablesCommand<ITable>, QueueCreationResult>
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly ICommandHandlerWithOutput<CreateJobTablesCommand<ITable>, QueueCreationResult> _decorated;
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateJobTablesCommandDecorator" /> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="decorated">The decorated.</param>
        public CreateJobTablesCommandDecorator(IConnectionInformation connectionInformation,
            ICommandHandlerWithOutput<CreateJobTablesCommand<ITable>, QueueCreationResult> decorated)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => decorated, decorated);
            _connectionInformation = connectionInformation;
            _decorated = decorated;
        }
        public QueueCreationResult Handle(CreateJobTablesCommand<ITable> command)
        {
            if (!DatabaseExists.Exists(_connectionInformation.ConnectionString))
            { //no db file, create
                var fileName = GetFileNameFromConnectionString.GetFileName(_connectionInformation.ConnectionString);
                SQLiteConnection.CreateFile(fileName.FileName);
            }
            try
            {
                return _decorated.Handle(command);
            }
            //if the queue already exists, return that status; otherwise, bubble the error
            catch (SQLiteException error)
            {
                if (error.ResultCode == SQLiteErrorCode.Error && error.Message.Contains("table") && error.Message.Contains("already exists"))
                {
                    return new QueueCreationResult(QueueCreationStatus.AttemptedToCreateAlreadyExists);
                }
                throw new DotNetWorkQueueException("Failed to create table",
                    error);
            }
        }
    }
}
