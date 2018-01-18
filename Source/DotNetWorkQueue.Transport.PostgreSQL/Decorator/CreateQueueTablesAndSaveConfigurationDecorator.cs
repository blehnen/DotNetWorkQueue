using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Decorator
{
    /// <inheritdoc />
    internal class CreateQueueTablesAndSaveConfigurationDecorator : ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<ITable>, QueueCreationResult>
    {
        private readonly ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<ITable>, QueueCreationResult> _decorated;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateQueueTablesAndSaveConfigurationDecorator"/> class.
        /// </summary>
        /// <param name="decorated">The decorated.</param>
        public CreateQueueTablesAndSaveConfigurationDecorator(ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<ITable>, QueueCreationResult> decorated)
        {
            Guard.NotNull(() => decorated, decorated);
            _decorated = decorated;
        }
        /// <inheritdoc />
        public QueueCreationResult Handle(CreateQueueTablesAndSaveConfigurationCommand<ITable> command)
        {
            try
            {
                return _decorated.Handle(command);
            }
            //if the queue already exists, return that status; otherwise, bubble the error
            catch (PostgresException error)
            {
                if (error.SqlState == "42P07" || error.SqlState == "42710")
                {
                    return new QueueCreationResult(QueueCreationStatus.AttemptedToCreateAlreadyExists);
                }
                throw new DotNetWorkQueueException($"Failed to create job table(s). SQL script was {error.Statement}",
                    error);
            }
        }
    }
}
