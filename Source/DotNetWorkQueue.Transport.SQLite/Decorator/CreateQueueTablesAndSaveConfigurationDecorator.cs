using System.Data.SQLite;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Decorator
{
    internal class CreateQueueTablesAndSaveConfigurationDecorator : ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<ITable>, QueueCreationResult>
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<ITable>, QueueCreationResult> _decorated;

        public CreateQueueTablesAndSaveConfigurationDecorator(IConnectionInformation connectionInformation, 
            ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<ITable>, QueueCreationResult> decorated)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => decorated, decorated);
            _connectionInformation = connectionInformation;
            _decorated = decorated;
        }
        public QueueCreationResult Handle(CreateQueueTablesAndSaveConfigurationCommand<ITable> command)
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
